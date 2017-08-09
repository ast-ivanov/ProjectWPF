namespace Bars.BFO.FNS.Justification.Service.Agreement.FkDocumentAgreements.Customizer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    using Bars.B4;
    using Bars.B4.DataAccess;
    using Bars.B4.Modules.Agreement.AgreementStates;
    using Bars.B4.Modules.Agreement.Entities;
    using Bars.B4.Modules.Agreement.Enums;
    using Bars.B4.Modules.Agreement.Interface;
    using Bars.B4.Modules.Agreement.Models;
    using Bars.B4.Modules.Agreement.Service;
    using Bars.B4.Modules.Nsi.DictionaryMetadata.Interfaces.Repositories;
    using Bars.B4.Modules.Nsi.Interfaces.Domain;
    using Bars.B4.Modules.Nsi.Interfaces.Events;
    using Bars.B4.Utils;
    using Bars.BFO.BP.Nsi;
    using Bars.BFO.BP.Nsi.Custom.Entities;
    using Bars.BFO.BP.Nsi.Custom.Utils;
    using Bars.BFO.FNS.Justification.Entities.FkDocuments;
    using Bars.BFO.FNS.Justification.Entities.FkDocuments.LimitStaffs;
    using Bars.BFO.FNS.Justification.Enums;
    using Bars.BFO.FNS.Justification.Interfaces.DomainServices;

    using BarsGroup.CodeGuard;

    /// <summary>"ДФК - Правило для проверки возможности согласования документа 'Штатная численность'"</summary>
    [Display("ДФК - Правило для проверки возможности согласования документа 'Штатная численность'")]
    [Description(@"Проверяет что значения численности не превышают предельное значение.")]
    public class FkDocumentRegularStaffingCanAgreementCheckCustomizer : EmptyAgreementCustomizer
    {
        public const string Code = "Justification.FkDocumentRegularStaffingCanAgreementCheckCustomizer";

        public IAgrStateValueDomainService AgrStateValRepo { get; set; }

        public IFkDocumentDomainService VersionedDocumentDomainService { get; set; }

        public IDomainService<LimitStaff> LimitStaffDomainService { get; set; }

        public IDomainService<RegularStaffing> RegularStaffingDomainService { get; set; }

        public IDomainService<NotCivilServant> NotCivilServantDomainService { get; set; }

        public IDomainService<WorkingProf> WorkingProfDomainService { get; set; }

        public INsiDomainService<Department> DepartmentDomainService { get; set; }

        public IEventQueryService EventQueryService { get; set; }

        public IDictionaryMetadataRepository MetadataRepository { get; set; }
        
        public IEventsDomainService EventsDomainService { get; set; }
        
        public IDomainService<LimitStaffByRbsDetalization> LimitStaffByRbsDetalizationDomainService { get; set; }
        
        private readonly EBudgetCycleYear[] _budgetCycleYears = { EBudgetCycleYear.FirstYear, EBudgetCycleYear.SecondYear, EBudgetCycleYear.ThirdYear };
        
        public override IUserTaskAdditionalInfo GetUserTaskAdditionalInfo(AgrStateValue agreementStateValue, PersistentObject entity, AgrStep resolution, UserTaskInstanceInfo taskInstanceId)
        {
            var regularStaffDocument = entity as FkDocument;
            if (regularStaffDocument == null || regularStaffDocument.FkDocumentType != FkDocumentType.RegularStaffing)
            {
                return null;
            }

            var errors = ValidateFkDocument(regularStaffDocument);
            if (!errors.IsEmpty())
            {
                var result = new UserTaskAddInfo
                {
                    RestrictProcess = true,
                    RestrictReason = string.Join("<br>", errors)
                };
                return result;
            }

            return null;
        }

        private List<string> ValidateFkDocument(FkDocument regularStaffDocument)
        {
            Guard.That(regularStaffDocument).IsNotNull();
            
            var result = new List<string>();

            var docDepartment = regularStaffDocument.Department;
            
            var grbsDepartment = GetGrbsDepartment();

            if (grbsDepartment == null)
            {
                result.Add($"В событии {ChainEventCodes.DepartmentGrbs} нет учреждений");
                return result;
            }
            
            var regularStaffingSumModel = GetRegularStaffingSumModel(regularStaffDocument);

            // учреждение имеет дочерние
            var hasChildrens = DepartmentDomainService.GetAll(DateTime.Now).Any(department => department.ParentId == docDepartment.Id);
            // учреждение => ФНС
            var isDocAuthorDepartmentFns = docDepartment.MetaId == grbsDepartment.MetaId;
            // родитель учреждения => ФНС
            var isDocAuthorParentDepartmentFns = docDepartment.ParentMetaId.HasValue && docDepartment.ParentMetaId == grbsDepartment.MetaId;
            
            var limitStaffSumModel = new Dictionary<EBudgetCycleYear, LimitStaffingSumModel>();
            FkDocumentType limitStaffDocumentType = 0;

            // определяем к какому типу относится Учреждение автора документа:
            // 1. Учреждение 3-го уровня иерархии и имеет родительскую запись 2-го уровня (не сама ФНС и его родитель не ФНС)
            if (!isDocAuthorDepartmentFns && !isDocAuthorParentDepartmentFns)
            {
                // получаем родительское учреждение документа "Штатная численность" и находим для него соответствующую детализацию документа "Предельные показатели по персоналу РБС"
                var departmentParentMetaId = regularStaffDocument.Department.ParentMetaId.Value;
                var parentDepartment = DepartmentDomainService.GetByMetaId(departmentParentMetaId, DateTime.Now);
                
                limitStaffSumModel = GetLimitStaffByRbsSumModel(regularStaffDocument.BudgetCycle, parentDepartment, regularStaffDocument.Department);
                limitStaffDocumentType = FkDocumentType.LimitStaffByRbs;
            }
            // 2. Учреждение 2-го уровня иерархии, у которого есть дочерние записи 3-го уровня (т.е. его родитель ФНС и есть дочерние)
            if (isDocAuthorParentDepartmentFns && hasChildrens)
            {
                // для учреждения документа "Штатная численность" находим соответствующую детализацию документа "Предельные показатели по персоналу РБС"
                limitStaffSumModel = GetLimitStaffByRbsSumModel(regularStaffDocument.BudgetCycle, regularStaffDocument.Department, regularStaffDocument.Department);
                limitStaffDocumentType = FkDocumentType.LimitStaffByRbs;
            }
            // 3. Учреждение 2-го уровня, у которого нет дочерних записей, или Учреждение, указанное в событии DepartmentGRBS (либо ФНС, либо родитель ФНС и дочерних нет)
            if (isDocAuthorDepartmentFns || isDocAuthorParentDepartmentFns && !hasChildrens)
            {
                // для учреждения документа "Штатная численность" находим соответствующий документ "Предельные показатели по персоналу"
                limitStaffSumModel = GetLimitStaffSumModel(regularStaffDocument.BudgetCycle, regularStaffDocument.Department);
                limitStaffDocumentType = FkDocumentType.LimitStaff;
            }
            
            //проверка на наличие документа предельных показателей в данном бц
            if (limitStaffSumModel.IsNull())
            {
                result.Add($"Не найден подходящий документ \"{limitStaffDocumentType.GetDisplayName()}\"! Отправка на согласование невозможна");
                return result;
            }

            //проверка на наличие записей предельных показателей в документе
            if (limitStaffSumModel.IsEmpty())
            {
                result.Add($"Не найдено строк учреждения в документе \"{limitStaffDocumentType.GetDisplayName()}\"! Отправка на согласование невозможна");
                return result;
            }

            return CompareAmounts(regularStaffingSumModel, limitStaffSumModel, limitStaffDocumentType, regularStaffDocument.BudgetCycle);
        }       

        private List<string> CompareAmounts(Dictionary<EBudgetCycleYear, RegularStaffingSumModel> regularStaffSumModel,
                                            Dictionary<EBudgetCycleYear, LimitStaffingSumModel> limitStaffSumModel,
                                            FkDocumentType limitStaffDocumentType,
                                            BudgetCycle budgetCycle)
        {
            Guard.That(regularStaffSumModel).IsNotEmpty();
            Guard.That(limitStaffSumModel).IsNotEmpty();
            
            var result = new List<string>();
            var budgetCycleYearDictionary = GetBudgetCycleYearDictionary(budgetCycle);

            foreach (var year in _budgetCycleYears)
            {
                var limitStaffingSumModel = limitStaffSumModel.ContainsKey(year)
                                                ? limitStaffSumModel[year]
                                                : null;
                var regularStaffingSumModel = regularStaffSumModel.ContainsKey(year)
                                                  ? regularStaffSumModel[year]
                                                  : null;
                var recordsInYearErrors = CheckRecordsInYear(regularStaffingSumModel, limitStaffingSumModel, budgetCycleYearDictionary[year], limitStaffDocumentType);
                
                if (!recordsInYearErrors.IsEmpty())
                {
                    result.AddRange(recordsInYearErrors);
                    continue;
                }

                var civilLimitAmount = limitStaffingSumModel.CivilLimitAmount;
                var notCivilLimitAmount = limitStaffingSumModel.NotCivilLimitAmount;
                var mopLimitAmount = limitStaffingSumModel.MopLimitAmount;

                var regularStaffingPersonCount = regularStaffingSumModel.RegularStaffingPersonCount;
                var notCivilServantPersonCount = regularStaffingSumModel.NotCivilServantPersonCount;
                var workProfWorkersCount = regularStaffingSumModel.WorkProfWorkersCount;

                if (regularStaffingPersonCount > civilLimitAmount)
                {
                    result.Add(
                        $"Предельная численность госслужащих превышает предельный показатель на " +
                        $"{(regularStaffingPersonCount - civilLimitAmount):F0} чел. по {budgetCycleYearDictionary[year]} г.");
                }

                if (notCivilServantPersonCount > notCivilLimitAmount)
                {
                    result.Add(
                        $"Предельная численность не госслужащих превышает предельный показатель на " +
                        $"{(notCivilServantPersonCount - notCivilLimitAmount):F0} чел. по {budgetCycleYearDictionary[year]} г.");
                }

                if (workProfWorkersCount > mopLimitAmount)
                {
                    result.Add(
                        $"Предельная численность персонала профессии рабочих превышает предельный показатель " +
                        $"на {(workProfWorkersCount - mopLimitAmount):F0} чел. по {budgetCycleYearDictionary[year]} г.");
                }
            }
            return result;
        }

        /// <summary>Проверка на наличие записей в документах ШЧ и Предельные показатели по персоналу* в году</summary>
        /// <returns>Список ошибок</returns>
        private List<string> CheckRecordsInYear(RegularStaffingSumModel regularStaffingSumModel,
                                                LimitStaffingSumModel limitStaffingSumModel, 
                                                int year, 
                                                FkDocumentType limitStaffDocType)
        {
            var result = new List<string>();
            if (regularStaffingSumModel?.RegularStaffingPersonCount == null)
            {
                result.Add(
                    $"Не заполнена численность по \"Госслужащие\" документа \"{FkDocumentType.RegularStaffing.GetDisplayName()}\" по {year} г.");
            }
            if (regularStaffingSumModel?.NotCivilServantPersonCount == null)
            {
                result.Add(
                    $"Не заполнена численность по \"Не госслужащие\" документа \"{FkDocumentType.RegularStaffing.GetDisplayName()}\" по {year} г.");
            }
            if (regularStaffingSumModel?.WorkProfWorkersCount == null)
            {
                result.Add(
                    $"Не заполнена численность по \"НСОТ\" документа \"{FkDocumentType.RegularStaffing.GetDisplayName()}\" по {year} г.");
            }
            if (limitStaffingSumModel?.CivilLimitAmount == null)
            {
                result.Add(
                    $"Не заполнена численность по \"Госслужащие\" документа \"{limitStaffDocType.GetDisplayName()}\" по {year} г.");
            }
            if (limitStaffingSumModel?.NotCivilLimitAmount == null)
            {
                result.Add(
                    $"Не заполнена численность по \"Не госслужащие\" документа \"{limitStaffDocType.GetDisplayName()}\" по {year} г.");
            }
            if (limitStaffingSumModel?.MopLimitAmount == null)
            {
                result.Add(
                    $"Не заполнена численность по \"МОП\" документа \"{limitStaffDocType.GetDisplayName()}\" по {year} г.");
            }

            return result;
        }
        
        private Dictionary<EBudgetCycleYear, LimitStaffingSumModel> GetLimitStaffByRbsSumModel(BudgetCycle budgetCycle, Department docDepartment, Department rpbsDepartment)
        {
            Guard.That(rpbsDepartment).IsNotNull();
            
            var limitStaffDocId = GetApprovedLimitStaffDocId(budgetCycle, FkDocumentType.LimitStaffByRbs, docDepartment);
            
            return limitStaffDocId != null 
                       ? LimitStaffByRbsDetalizationDomainService.GetAll()
                                                                 .Where(x => x.LimitStaffRbsGrbsRelation.FkDocument.Id == limitStaffDocId.Value)
                                                                 .Where(x => x.PbsDepartment.MetaId == rpbsDepartment.MetaId)
                                                                 .ToDictionary(
                                                                     x => x.LimitStaffRbsGrbsRelation.LimitStaff.BudgetCycleYear,
                                                                     x => new LimitStaffingSumModel
                                                                          {
                                                                              CivilLimitAmount = x.CivilLimitAmount,
                                                                              NotCivilLimitAmount = x.NotCivilLimitAmount,
                                                                              MopLimitAmount = x.MopLimitAmount
                                                                          })
                       : null;
        }

        private Dictionary<EBudgetCycleYear, LimitStaffingSumModel> GetLimitStaffSumModel(BudgetCycle budgetCycle, Department docDepartment)
        {
            Guard.That(docDepartment).IsNotNull();
            
            var limitStaffDocId = GetApprovedLimitStaffDocId(budgetCycle, FkDocumentType.LimitStaff, docDepartment);

            return limitStaffDocId != null
                       ? LimitStaffDomainService.GetAll()
                                                .Where(x => x.FkDocument.Id == limitStaffDocId.Value)
                                                .Where(x => x.RpbsDepartment.MetaId == docDepartment.MetaId)
                                                .ToDictionary(
                                                    x => x.BudgetCycleYear,
                                                    x => new LimitStaffingSumModel
                                                         {
                                                             CivilLimitAmount = x.CivilLimitAmount,
                                                             NotCivilLimitAmount = x.NotCivilLimitAmount,
                                                             MopLimitAmount = x.MopLimitAmount
                                                         })
                       : null;
        }
        
        private long? GetApprovedLimitStaffDocId(BudgetCycle budgetCycle, FkDocumentType fkDocType, Department docDepartment)
        {
            Guard.That(budgetCycle).IsNotNull();
            Guard.That(docDepartment).IsNotNull();
            
            var notApprovedStates = new[]
                                    {
                                        AgreementStateEnum.AgreementProgress,
                                        AgreementStateEnum.NeedAgreement,
                                        AgreementStateEnum.NotAgreed
                                    };

            var notApprovedIdsQuery = AgrStateValRepo.GetAll<FkDocument>()
                                                     .Where(x => notApprovedStates.Contains(x.Value))
                                                     .Select(x => x.EntityId);

            var limitStaffDocId = VersionedDocumentDomainService.GetAll()
                .Where(x => x.BudgetCycle.Id == budgetCycle.Id)
                .Where(x => !notApprovedIdsQuery.Any(y => y == x.Id))
                .Where(x => x.FkDocumentType == fkDocType)
                .Select(x => (long?)x.Id)
                .FirstOrDefault();
            return limitStaffDocId;
        }
        
        private Dictionary<EBudgetCycleYear, RegularStaffingSumModel> GetRegularStaffingSumModel(FkDocument regularStaffDocument)
        {
            var regularStaffingPersonList = GetLimitAmountByYear(RegularStaffingDomainService, regularStaffDocument);
            
            var notCivilServantPersonList = GetLimitAmountByYear(NotCivilServantDomainService, regularStaffDocument);
            
            var workProfWorkersList = GetLimitAmountByYear(WorkingProfDomainService, regularStaffDocument);

            var regularStaffingSumModel = new Dictionary<EBudgetCycleYear, RegularStaffingSumModel>();

            foreach (EBudgetCycleYear year in _budgetCycleYears)
            {
                var regularStaffingPersonCount = regularStaffingPersonList.Where(x => x.BudgetCycleYear == year);
                
                var notCivilServantPersonCount = notCivilServantPersonList.Where(x => x.BudgetCycleYear == year);
                
                var workProfWorkersCount = workProfWorkersList.Where(x => x.BudgetCycleYear == year);
                
                if (!regularStaffingPersonCount.IsEmpty() || !notCivilServantPersonCount.IsEmpty() || !workProfWorkersCount.IsEmpty())
                {
                    regularStaffingSumModel.Add(year, new RegularStaffingSumModel
                                                      {
                                                          RegularStaffingPersonCount = regularStaffingPersonCount.Sum(x => x.LimitAmount),
                                                          NotCivilServantPersonCount = notCivilServantPersonCount.Sum(x => x.LimitAmount),
                                                          WorkProfWorkersCount = workProfWorkersCount.Sum(x => x.LimitAmount)
                                                      });
                }
            }
            
            return regularStaffingSumModel;
        }

        private List<LimitAmountByYearModel> GetLimitAmountByYear<TRegularStaffingEntity>(IDomainService<TRegularStaffingEntity> domainService, FkDocument regularStaffDocument)
            where TRegularStaffingEntity : BaseRegularStaffingEntity
        {
            Guard.That(domainService).IsNotNull();
            Guard.That(regularStaffDocument).IsNotNull();
            
            var regularStaffingPersonList = domainService.GetAll()
                                                         .Where(x => x.FkDocument.Id == regularStaffDocument.Id)
                                                         .Select(
                                                             x => new LimitAmountByYearModel
                                                                  {
                                                                      BudgetCycleYear = x.BudgetCycleYear,
                                                                      LimitAmount = x.LimitAmount
                                                                  })
                                                         .ToList();
            return regularStaffingPersonList;
        }

        private Department GetGrbsDepartment()
        {
            var chains = EventsDomainService.GetEventChains(ChainEventCodes.DepartmentGrbs).Select(x => x.Chain);

            var grbsDepartment = chains.Any()
                                     ? EventQueryService.FilterEntityByEvent(
                                         DepartmentDomainService.GetAll(DateTime.Now),
                                         ChainEventCodes.DepartmentGrbs,
                                         new Dictionary<string, Expression<Func<Department, Guid>>>(1)
                                         {
                                             { MetadataRepository.Get(typeof(Department)).Code, x => x.MetaId }
                                         },
                                         DateTime.UtcNow).SingleOrDefault()
                                     : null;
            return grbsDepartment;
        }
        
        private Dictionary<EBudgetCycleYear, int> GetBudgetCycleYearDictionary(BudgetCycle budgetCycle)
        {         
            Guard.That(budgetCycle).IsNotNull();
            
            var dict = new Dictionary<EBudgetCycleYear, int>();
            for (var i = 0; i < _budgetCycleYears.Length; i++)
            {
                dict.Add(_budgetCycleYears[i], budgetCycle.StartYear + 1 + i);
            }
            
            return dict;
        }
        
        private class LimitAmountByYearModel
        {
            public EBudgetCycleYear BudgetCycleYear { get; set; }
            
            public decimal LimitAmount { get; set; }
        }
        
        private class LimitStaffingSumModel
        {            
            public int? CivilLimitAmount { get; set; }

            public int? NotCivilLimitAmount { get; set; }
            
            public int? MopLimitAmount { get; set; }
        }
        
        private class RegularStaffingSumModel
        {
            public decimal? RegularStaffingPersonCount { get; set; }
            
            public decimal? NotCivilServantPersonCount { get; set; }
            
            public decimal? WorkProfWorkersCount { get; set; }
        }
    }
}