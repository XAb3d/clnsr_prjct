using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CleanserBlazorUI.Data;
public class DataManagementService
{
    private string text_value_seperator { get; set; } = "&&&___&&&";
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    public DataManagementService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    // Returns all reference records for this subscriber.
    // The reference always holds the cumulative known state — every unique
    // (AccNum, CustomerID, DisbursementDate) ever seen for this subscriber.
    public async Task<List<IndividualRef>> GETReferenceData_IND(string fileShortName)
    {
        var subscriber = await GetFileShortCodeFromFileName(fileShortName);
        return await _context.IndividualsData
            .Where(p => p.SubscriberCode == subscriber)
            .ToListAsync();
    }
    public async Task<List<BusinessRef>> GETReferenceData_BUS(string fileShortName)
    {
        var subscriber = await GetFileShortCodeFromFileName(fileShortName);
        return await _context.BusinessesData
            .Where(p => p.SubscriberCode == subscriber)
            .ToListAsync();
    }
    
    
    
    // ── IND Upsert ────────────────────────────────────────────────────────────
    // Upsert key: (SubscriberCode, AccNum, CustomerID, DisbursementDate)
    // - New records    → INSERT
    // - Existing rows  → enrich ID fields where DB is empty + file has value
    // - Absent records → left as-is (historical, never deleted)
    // Returns changelog: every (AccNum, CustomerID, DisbDate, FieldName) enriched this run.
    public async Task<List<(string AccNum, string CustomerID, string DisbDate, string FieldAdded)>>
        SaveExcelDataToDatabaseInd(IEnumerable<DBIndividualContext> dataFromExcel, string fileShortName)
    {
        var subscriber = await GetFileShortCodeFromFileName(fileShortName);
        var now        = DateTime.Now;
        var changelog  = new List<(string, string, string, string)>();

        var existing = await _context.IndividualsData
            .Where(r => r.SubscriberCode == subscriber)
            .ToListAsync();

        var existingIndex = existing.ToDictionary(
            r => (Norm(r.CreditFacilityAccNum), Norm(r.CustomerID), Norm(r.DisbursementDate)),
            r => r
        );

        var toInsert = new List<IndividualRef>();
        var toUpdate = new List<IndividualRef>();

        foreach (var item in dataFromExcel)
        {
            var key = (Norm(item.CreditFacilityAccNum),
                       Norm(item.CustomerID),
                       Norm(item.DisbursementDate));

            if (existingIndex.TryGetValue(key, out var dbRow))
            {
                // EF Core entity properties can't be passed as ref directly.
                // Copy to locals, enrich, write back.
                string? natID = dbRow.NatIDNum, votersID = dbRow.VotersIDNum,
                        driverLic = dbRow.DriverLicNum, passport = dbRow.PassportNum,
                        ssNum = dbRow.SSNum, ezwich = dbRow.EzwichNum, otherID = dbRow.OtherIDNum;

                bool changed = false;
                changed |= EnrichField(ref natID,     item.NatIDNum,     "NatIDNum",     key.Item1, key.Item2, key.Item3, changelog);
                changed |= EnrichField(ref votersID,  item.VotersIDNum,  "VotersIDNum",  key.Item1, key.Item2, key.Item3, changelog);
                changed |= EnrichField(ref driverLic, item.DriverLicNum, "DriverLicNum", key.Item1, key.Item2, key.Item3, changelog);
                changed |= EnrichField(ref passport,  item.PassportNum,  "PassportNum",  key.Item1, key.Item2, key.Item3, changelog);
                changed |= EnrichField(ref ssNum,     item.SSNum,        "SSNum",        key.Item1, key.Item2, key.Item3, changelog);
                changed |= EnrichField(ref ezwich,    item.EzwichNum,    "EzwichNum",    key.Item1, key.Item2, key.Item3, changelog);
                changed |= EnrichField(ref otherID,   item.OtherIDNum,   "OtherIDNum",   key.Item1, key.Item2, key.Item3, changelog);

                if (changed)
                {
                    dbRow.NatIDNum = natID; dbRow.VotersIDNum = votersID;
                    dbRow.DriverLicNum = driverLic; dbRow.PassportNum = passport;
                    dbRow.SSNum = ssNum; dbRow.EzwichNum = ezwich; dbRow.OtherIDNum = otherID;
                    dbRow.LastUpdatedDate = now;
                    toUpdate.Add(dbRow);
                }
                if (!string.IsNullOrWhiteSpace(item.DateOfBirth))
                    dbRow.DateOfBirth = item.DateOfBirth;
            }
            else
            {
                toInsert.Add(new IndividualRef
                {
                    SubscriberCode       = subscriber,
                    CreditFacilityAccNum = item.CreditFacilityAccNum        ?? string.Empty,
                    CustomerID           = item.CustomerID                  ?? string.Empty,
                    DisbursementDate     = item.DisbursementDate             ?? string.Empty,
                    DateOfBirth          = item.DateOfBirth                  ?? string.Empty,
                    NatIDNum             = item.NatIDNum                    ?? string.Empty,
                    VotersIDNum          = item.VotersIDNum                 ?? string.Empty,
                    DriverLicNum         = item.DriverLicNum                ?? string.Empty,
                    PassportNum          = item.PassportNum                 ?? string.Empty,
                    SSNum                = item.SSNum                       ?? string.Empty,
                    EzwichNum            = item.EzwichNum                   ?? string.Empty,
                    OtherIDNum           = item.OtherIDNum                  ?? string.Empty,
                    CurrenVersion        = 1,
                    CreatedDate          = now,
                    LastUpdatedDate      = now
                });
            }
        }

        const int batchSize = 10_000;
        for (int i = 0; i < toInsert.Count; i += batchSize)
            await BulkInsertBatchIND(toInsert.Skip(i).Take(batchSize).ToList());
        if (toUpdate.Count > 0)
            await _context.BulkUpdateAsync(toUpdate, new BulkConfig { BatchSize = 4000 });

        return changelog;
    }


    // ── Overload for CLEAN path — accepts IndividualContext (has .Data properties) ─
    public async Task<List<(string AccNum, string CustomerID, string DisbDate, string FieldAdded)>>
        SaveExcelDataToDatabaseInd(IEnumerable<IndividualContext> dataFromExcel, string fileShortName)
    {
        var mapped = dataFromExcel.Select(r => new DBIndividualContext
        {
            CreditFacilityAccNum = r.CreditFacilityAccNum?.Data ?? string.Empty,
            CustomerID           = r.CustomerID?.Data           ?? string.Empty,
            DisbursementDate     = r.DisbursementDate?.Data     ?? string.Empty,
            DateOfBirth          = r.DateOfBirth?.Data          ?? string.Empty,
            NatIDNum             = r.NatIDNum?.Data             ?? string.Empty,
            VotersIDNum          = r.VotersIDNum?.Data          ?? string.Empty,
            DriverLicNum         = r.DriverLicNum?.Data         ?? string.Empty,
            PassportNum          = r.PassportNum?.Data          ?? string.Empty,
            SSNum                = r.SSNum?.Data                ?? string.Empty,
            EzwichNum            = r.EzwichNum?.Data            ?? string.Empty,
            OtherIDNum           = r.OtherIDNum?.Data           ?? string.Empty,
        });
        return await SaveExcelDataToDatabaseInd(mapped, fileShortName);
    }

    // ── BUS Upsert ────────────────────────────────────────────────────────────
    public async Task SaveExcelDataToDatabaseBus(IEnumerable<DBBusinessContext> dataFromExcel, string fileShortName)
    {
        var subscriber = await GetFileShortCodeFromFileName(fileShortName);
        var now        = DateTime.Now;

        var existing = await _context.BusinessesData
            .Where(r => r.SubscriberCode == subscriber)
            .ToListAsync();

        var existingIndex = existing.ToDictionary(
            r => (Norm(r.CreditFacilityAccNum), Norm(r.CustomerID), Norm(r.DisbursementDate)),
            r => r
        );

        var toInsert = new List<BusinessRef>();
        var toUpdate = new List<BusinessRef>();

        foreach (var item in dataFromExcel)
        {
            var key = (Norm(item.Facilityaccnum),
                       Norm(item.CustomerID),
                       Norm(item.DisbursementDate));

            if (existingIndex.TryGetValue(key, out var dbRow))
            {
                if (!string.IsNullOrWhiteSpace(item.DateOfBirth))
                    dbRow.DateOfBirth = item.DateOfBirth;
                dbRow.LastUpdatedDate = now;
                toUpdate.Add(dbRow);
            }
            else
            {
                toInsert.Add(new BusinessRef
                {
                    SubscriberCode       = subscriber,
                    CreditFacilityAccNum = item.Facilityaccnum  ?? string.Empty,
                    CustomerID           = item.CustomerID       ?? string.Empty,
                    DisbursementDate     = item.DisbursementDate ?? string.Empty,
                    DateOfBirth          = item.DateOfBirth      ?? string.Empty,
                    CurrenVersion        = 1,
                    CreatedDate          = now,
                    LastUpdatedDate      = now
                });
            }
        }

        const int batchSize = 10_000;
        for (int i = 0; i < toInsert.Count; i += batchSize)
            await BulkInsertBatchBUS(toInsert.Skip(i).Take(batchSize).ToList());
        if (toUpdate.Count > 0)
            await _context.BulkUpdateAsync(toUpdate, new BulkConfig { BatchSize = 4000 });
    }


    // ── BUS overload for CLEAN path — accepts BusinessContext (has .Data properties) ─
    public async Task SaveExcelDataToDatabaseBus(IEnumerable<BusinessContext> dataFromExcel, string fileShortName)
    {
        var mapped = dataFromExcel.Select(r => new DBBusinessContext
        {
            Facilityaccnum   = r.Facilityaccnum?.Data   ?? string.Empty,
            CustomerID       = r.CustomerID?.Data       ?? string.Empty,
            DisbursementDate = r.DisbursementDate?.Data ?? string.Empty,
            DateOfBirth      = r.DateOfBirth?.Data      ?? string.Empty,
        });
        await SaveExcelDataToDatabaseBus(mapped, fileShortName);
    }

    private bool EnrichField(
        ref string? dbField, string? fileValue,
        string fieldName, string accNum, string custId, string disbDate,
        List<(string, string, string, string)> changelog)
    {
        if (!string.IsNullOrWhiteSpace(fileValue) && string.IsNullOrWhiteSpace(dbField))
        {
            dbField = fileValue;
            changelog.Add((accNum, custId, disbDate, fieldName));
            return true;
        }
        return false;
    }

    private string Norm(string? v) =>
        string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim().ToUpperInvariant();



    //INSERT REFERENCE BUSINESS DATA
    public async Task Initialize_SaveExcelDataToDatabaseInd(IEnumerable<DBIndividualContext> dataFromExcel, string fileShortName)
    {
        //var subscriber = await GetFileShortCodeFromFileName(fileShortName);
        var subscriber = await GetFileShortCodeFromFileName(fileShortName);
        const int batchSize = 10_000;
        var currentBatch = new List<DBIndividualContext>(batchSize);
        var currentBatchDB = new List<IndividualRef>();

        var _createdDate = DateTime.Now;
        var currentVersion = await GetIndividualMaxversion();
        foreach (var item in dataFromExcel)
        {
            currentBatchDB.Add(new IndividualRef
            {
                CreditFacilityAccNum = item.CreditFacilityAccNum,
                CustomerID = item.CustomerID,
                DisbursementDate = item.DisbursementDate,
                DateOfBirth = item.DateOfBirth,
                SubscriberCode = subscriber,
                CurrenVersion = currentVersion,
                CreatedDate = _createdDate
            });

            if (currentBatchDB.Count >= batchSize)
            {
                await BulkInsertBatchIND(currentBatchDB);
                currentBatch.Clear();
                currentBatchDB.Clear();
            }
        }

        // Insert remaining records
        if (currentBatchDB.Count > 0)
        {
            await BulkInsertBatchIND(currentBatchDB);
        }
    }
    public async Task Initialize_SaveExcelDataToDatabaseBus(IEnumerable<DBBusinessContext> dataFromExcel, string fileShortName)
    {
        //var subscriber = await GetFileShortCodeFromFileName(fileShortName);
        var subscriber = await GetFileShortCodeFromFileName(fileShortName);
        const int batchSize = 10_000;
        var currentBatch = new List<DBBusinessContext>(batchSize);
        var currentBatchDB = new List<BusinessRef>();

        var _createdDate = DateTime.Now;
        var currentVersion = await GetBusinessMaxversion();
        foreach (var item in dataFromExcel)
        {
            currentBatchDB.Add(new BusinessRef
            {
                CreditFacilityAccNum = item.Facilityaccnum,
                CustomerID = item.CustomerID,
                DisbursementDate = item.DisbursementDate,
                DateOfBirth = item.DateOfBirth,
                SubscriberCode = subscriber,
                CurrenVersion = currentVersion,
                CreatedDate = _createdDate
            });

            if (currentBatchDB.Count >= batchSize)
            {
                await BulkInsertBatchBUS(currentBatchDB);
                currentBatch.Clear();
                currentBatchDB.Clear();
            }
        }

        // Insert remaining records
        if (currentBatchDB.Count > 0)
        {
            await BulkInsertBatchBUS(currentBatchDB);
        }
    }
    
    
    
    public async Task<string> GetFileShortCodeFromFileNameDB(string fileShortName)
    {
        var _filename = fileShortName.Split('_')[0];
        _filename = GetFirstPartExcludeLastPart(_filename, 4);
        var subscriber = await GetSubscriberUserInfos(_filename);
        return subscriber;
    }
    public async Task<string> GetSubscribeX_or_Y(string fileShortName)
    {
        var _filename = fileShortName.Split('_')[0];
        _filename = GetFirstPartExcludeLastPart(_filename, 4);
        List<SubscribeContext> all_subscriberCategoryCode = await GetShortCodeFromSubscribeIDAsync();

        // Use null-conditional operator and null-coalescing operator to handle potential null values
        string scc = all_subscriberCategoryCode
            .FirstOrDefault(sub => sub.ShortName == _filename)?.SubCategoryCode ?? string.Empty;

        if (scc == "01")
        {
            scc = "X";
        }
        else
        {
            scc = "W";
        }
        return scc;
    }
    //Return only Shortname of a file
    public async Task<string> GetFileShortCodeFromFileName(string fileShortName)
    {
        var _filename = fileShortName.Split('_')[0];
        _filename = GetFirstPartExcludeLastPart(_filename, 4);
        return _filename;
    }
    public async Task<string> GetShortCodefromReferencing(string _filename, string type_Bus_or_ind)
    {
        var _shortcode = await GetFileShortCodeFromFileName(_filename);
        IndividualRef individualRef = new();
        BusinessRef businessRef = new();
        if (type_Bus_or_ind == "ind")
        {
            individualRef = _context.IndividualsData.FirstOrDefault(s => s.SubscriberCode == _shortcode) ?? new IndividualRef();
            return individualRef.SubscriberCode ?? string.Empty;
        }
        else if(type_Bus_or_ind == "bus")
        {
            businessRef = _context.BusinessesData.FirstOrDefault(s => s.SubscriberCode == _shortcode) ?? new BusinessRef();
            return businessRef.SubscriberCode ?? string.Empty;
        }
        else
        {
            return string.Empty;
        }
        
    }



    //SHARED FUNCTION INDIVIDUAL
    private async Task BulkInsertBatchIND(List<IndividualRef> batch)
    {
        await _context.BulkInsertAsync(batch, new BulkConfig
        {
            BatchSize = 4000,
            EnableStreaming = true,
            BulkCopyTimeout = 3600 // 1 hour
        });
    }
    //SHARED FUNCTION BUSINESS
    private async Task BulkInsertBatchBUS(List<BusinessRef> batch)
    {
        await _context.BulkInsertAsync(batch, new BulkConfig
        {
            BatchSize = 4000,
            EnableStreaming = true,
            BulkCopyTimeout = 3600 // 1 hour
        });
    }
    public async Task<int> GetIndividualMaxversion()
    {
        int maxId = await _context.IndividualsData
            .MaxAsync(s => (int?)s.CurrenVersion) ?? 0; // Handle null case

        return maxId + 1;
    }
    public async Task<int> GetBusinessMaxversion()
    {
        int maxId = await _context.BusinessesData
            .MaxAsync(s => (int?)s.CurrenVersion) ?? 0; // Handle null case

        return maxId + 1;
    }

    //GET EXTETNAL DATABASE TABLE (SUBSCRIBERS)
    public async Task<string> GetSubscriberUserInfos(string _shortname)
    {
        List<SubscribeContext> contexts = await GetShortCodeFromSubscribeIDAsync();
        List<string> shortCodes = contexts.Select(context => context.ShortName).ToList();
        string specificShortCode = shortCodes.FirstOrDefault(s => s == _shortname) ?? string.Empty;
        return specificShortCode;
    }
    public async Task<List<SubscribeContext>> GetShortCodeFromSubscribeIDAsync()
    {
        List<SubscribeContext> subscribeContexts = new();
        SubscribeContext subscribeContext;

        string connectionString = _configuration.GetConnectionString("SubscriberConnection")
            ?? throw new InvalidOperationException("Connection string 'SubscriberConnection' not found.");

        // Step 3: Create a SqlConnection object
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                // Step 4: Open the connection asynchronously
                await connection.OpenAsync();

                // Step 5: Define the SQL query
                string query = "SELECT * FROM [Subscriber].[Subscribers]"; // Fully qualified table name

                // Step 6: Execute the query asynchronously
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        // Step 7: Process the results
                        while (await reader.ReadAsync())
                        {
                            // Assuming the table has a column named "ShortName"
                            subscribeContext = new SubscribeContext
                            {
                                ShortName = reader.GetString(reader.GetOrdinal("ShortName")),
                                SubCategoryCode = reader.GetString(reader.GetOrdinal("SubCategoryCode"))
                            };
                            subscribeContexts.Add(subscribeContext);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        return subscribeContexts;
    }
    public async Task<List<string>> GetSubscriberUserInfosShotCode()
    {
        //return await GetShortCodeFromSubscribeIDAsync();
        // Assuming SubscribeContext has a 'ShortCode' property
        List<SubscribeContext> contexts = await GetShortCodeFromSubscribeIDAsync();
        return contexts.Select(context => context.ShortName).ToList();
    }





    //*****SettingsClass*******************
    public async Task AddSettings(SettingsClass _item, SettingsDataType settingsDataType)
    {
        var item = _item;
        item.DataType = settingsDataType;
        await _context.Settings.AddAsync(item);
        await _context.SaveChangesAsync();
    }
    public async Task DeleteSetting(SettingsClass item, SettingsDataType settingsDataType)
    {
        var itemToRemove = await _context.Settings.FirstOrDefaultAsync(s => s.Value == item.Value && s.DataType == settingsDataType);
        _context.Settings.Remove(itemToRemove);
        await _context.SaveChangesAsync();
    }
    public async Task<List<SettingsClass>> GetAllSettingsAsync(SettingsDataType settingsDataType)
    {
        return await _context.Settings.Where(s => s.DataType == settingsDataType).OrderBy(s => s.Value).ToListAsync();
    }
    public async Task<bool> GetSettingOne(string item, SettingsDataType settingsDataType)
    {
        return await _context.Settings.AnyAsync(s => s.Value == item && s.DataType == settingsDataType);
    }
    public async Task AddSettingsBulk(List<SettingsClass> item)
    {
        if (item.Count <= 0)
            return;
        await _context.Settings.AddRangeAsync(item);
        await _context.SaveChangesAsync();
    }
    private readonly string[] businessKeywords = {
      "CHURCH", "EMBASSY", "COMPANY", "BANK", "SCHOOL", "LTD", "LIMITED", "HOSPITAL",
        "CENTER", "CENTRE", "COMMUNICATION", "ENTERPRISE", "WORKS", "INSTITUTE", "DEV’T",
        "BUSINESS", "BUSINESSES", "INFORMATION", "SERVICE","SERVICES", "FIRM", "TRAINING", "HOTEL","AUTO","AUTOS","SCH","KIDS",
        "& CO", "& SONS", "VENTURES", "PRINTING", "OFFICE","INTERNATIONAL","GPRTU","TUC","MINISTRY","GROUP","WITH","GRP"," INT ",
        "CONSTRUCTION","SMART","LOAN","ENTRPRISE","SCHEME","AUTHORITHY","ASSOCIATION", "LOANS", "STAFF", "CONTROLLER", "EMPLOYEE",
        "COLLEGE", "ACCOUNT", "GHANA", "MOTORS", "AGENCY", "ENVIRONMENTAL", "UNIVERSITY", "PERSONAL", "UNION", "CO-OPERATIVE",
        "CO OPERATIVE", "COOPERATIVE"
    };
    public async Task<List<string>> InitializeSettingsDBAsync()
    {
        var BusinessNamesSettings = await GetAllSettingsAsync(SettingsDataType.BusinessName);
        if (BusinessNamesSettings.Count <= 0)
        {
            foreach (var item in businessKeywords)
            {
               var settingsClass = new SettingsClass() { Value = item, DataType = SettingsDataType.BusinessName };
                BusinessNamesSettings.Add(settingsClass);
            }
            await AddSettingsBulk(BusinessNamesSettings);
        }
        var settings = await GetAllSettingsAsync(SettingsDataType.BusinessName);
        List<string> settingAsAnArray = new();
        foreach (var item in settings)
        {
            settingAsAnArray.Add(item.Value ?? string.Empty);
        }
        return settingAsAnArray;
    }
  
    public string GetFirstPartExcludeLastPart(string input, int n)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= n)
            return string.Empty;

        return input[..^n]; // Using range operator to remove last n characters
    }

    public async Task BUS_NUM_AddSettings(BusSettNormalizer _item)
    {
        if (_item == null) return;
        var existingItem = await _context.BusinessClassNormalizer.FirstOrDefaultAsync(s => s.ShortValue == _item.ShortValue);
        DateTime date = new DateTime();
        if (existingItem == null)
        {
            _item.DateCreated = date.Date;
            _item.DateModified = date.Date;
            await _context.BusinessClassNormalizer.AddAsync(_item);
        }
        await _context.SaveChangesAsync();
    }

    //*****BUS_NORM_SettingsClass*******************

    public async Task BUS_NUM_DeleteSetting(BusSettNormalizer item)
    {
        if (item == null) return;
        var itemToRemove = await _context.BusinessClassNormalizer.FirstOrDefaultAsync(s => s.ShortValue == item.ShortValue);
        _context.BusinessClassNormalizer.Remove(itemToRemove);
        await _context.SaveChangesAsync();
    }
    //public async Task BUS_NUM_DeleteSetting(BusSettNormalizer item)
    //{
    //    if (item == null) return;
    //    var itemToRemove = await _context.Settings.FirstOrDefaultAsync(s => s.Id == item.Id);
    //    _context.Settings.Remove(itemToRemove);
    //    await _context.SaveChangesAsync();
    //}
    public async Task<List<BusSettNormalizer>> BUS_NUM_GetAllSettingsAsync()
    {
        return await _context.BusinessClassNormalizer.OrderBy(s => s.ShortValue).ToListAsync();
    }
    public async Task<bool> BUS_NUM_GetSettingOne(string shortvalue)
    {
        return await _context.BusinessClassNormalizer.AnyAsync(s => s.ShortValue == shortvalue);
    }
    public async Task BUS_NUM_AddSettingsBulk(List<BusSettNormalizer> item)
    {
        if (item.Count <= 0)
            return;
        await _context.BusinessClassNormalizer.AddRangeAsync(item);
        await _context.SaveChangesAsync();
    }

    private readonly Dictionary<string, string> businesssShortForms = new()
        {
            { "A/C", "ACCOUNT" },
            { "ACCT", "ACCOUNT" },
            { "ASSOC", "ASSOCIATION" },
            { "ASSO", "ASSOCIATION" },
            { "CO", "COMPANY" },
            { "COM", "COMPANY" },
            { "CONST", "CONSTRUCTION" },
            { "CON", "CONSULT" },
            { "CRDT", "CREDIT" },
            { "DEVT", "DEVELOPMENT" },
            { "DEV’T", "DEVELOPMENT" },
            { "ENG", "ENGINEERING" },
            { "GH", "GHANA" },
            { "(GH)", "GHANA" },
            { "GOV", "GOVERNMENT" },
            { "GOV’T", "GOVERNMENT" },
            { "GRP", "GROUP" },
            { "INT", "INTERNATIONAL" },
            { "INV", "INVESTMENT" },
            { "LT", "LIMITED" },
            { "LTD", "LIMITED" },
            { "MKTG", "MARKETING" },
            { "ND", "AND" },
            { "SCH", "SCHOOL" },
            { "SER", "SERVICE" },
            { "SERV", "SERVICE" },
            { "SYS", "SYSTEM" },
            { "TRAD", "TRADING" },
            { "WKS", "WORKS" },
            { "WK", "WORKS" },
            { "ENT", "ENTERPRISE" },
            { "HOSP", "HOSPITAL" }
        };
    public async Task<List<BusSettNormalizer>> BUS_NUM_InitializeSettingsDBAsyncNormalizer()
    {
        var BusinessNamesSettingsNormal = await BUS_NUM_GetAllSettingsAsync();
        if (BusinessNamesSettingsNormal.Count <= 0)
        {
            foreach (var item in businesssShortForms)
            {
                var settingsClassNormal = new BusSettNormalizer() { ShortValue = item.Key, LongValue = item.Value,DateCreated = DateTime.Now, DateModified = DateTime.Now, DataType = SettingsDataType.BusinessNamenormalizer };
                BusinessNamesSettingsNormal.Add(settingsClassNormal);
            }
            await BUS_NUM_AddSettingsBulk(BusinessNamesSettingsNormal);
        }
        var settingsNormal = await BUS_NUM_GetAllSettingsAsync();
        List<BusSettNormalizer> settingAsAnArray = new();

        foreach (var item in settingsNormal)
        {
            settingAsAnArray.Add(item);
        }
        return settingAsAnArray;
    }
    public async Task<Dictionary<string, string>> BUS_NUM_InitializeSettingsDBAsyncNormalizer_Home()
    {
        var BusinessNamesSettingsNormal = await BUS_NUM_GetAllSettingsAsync();
        if (BusinessNamesSettingsNormal.Count <= 0)
        {
            foreach (var item in businesssShortForms)
            {
                var settingsClassNormal = new BusSettNormalizer() { ShortValue = item.Key, LongValue = item.Value, DateCreated = DateTime.Now, DateModified = DateTime.Now, DataType = SettingsDataType.BusinessNamenormalizer };
                BusinessNamesSettingsNormal.Add(settingsClassNormal);
            }
            await BUS_NUM_AddSettingsBulk(BusinessNamesSettingsNormal);
        }
        var settingsNormal = await BUS_NUM_GetAllSettingsAsync();
        Dictionary<string, string> settingAsAnArray = new();

        foreach (var item in settingsNormal)
        {
            if (item.ShortValue != null && item.LongValue != null)
            {
                settingAsAnArray.Add(item.ShortValue, item.LongValue);
            }

        }
        return settingAsAnArray;
    }
}
