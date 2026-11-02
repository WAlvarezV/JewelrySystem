using System.Data;
using Microsoft.Data.SqlClient;
using Npgsql;

Console.WriteLine("==========================================================");
Console.WriteLine("  JewerlySystem Data Migrator (SQL Server -> PostgreSQL)  ");
Console.WriteLine("==========================================================");

var sqlConnStr = args.Length > 0 ? args[0] :
    "Server=(localdb)\\MSSQLLocalDB;Database=PomonaDb_Source;Trusted_Connection=True;TrustServerCertificate=True;";
var pgConnStr = args.Length > 1 ? args[1] :
    "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN;";

Console.WriteLine($"SQL Server: {sqlConnStr}");
Console.WriteLine($"PostgreSQL: {pgConnStr}");
Console.WriteLine();

using var sqlConn = new SqlConnection(sqlConnStr);
using var pgConn = new NpgsqlConnection(pgConnStr);

await sqlConn.OpenAsync();
await pgConn.OpenAsync();

Console.WriteLine("[1/4] Preparando base de datos PostgreSQL...");
// Truncar tablas de negocio para permitir re-ejecución limpia (idempotencia completa)
// Conservamos AppModules, MenuItems, RolePermissions y AspNetRoles intactos.
var truncateSql = @"
    TRUNCATE TABLE 
        ""Jewelry"", 
        ""Watches"", 
        ""Payments"", 
        ""Invoices"", 
        ""Contracts"", 
        ""Items"", 
        ""DailyRecords"", 
        ""Persons"", 
        ""ItemTypes"", 
        ""Brands"", 
        ""IdentificationTypes"",
        ""AspNetUserRoles"",
        ""AspNetUsers""
    CASCADE;";
using (var cmd = new NpgsqlCommand(truncateSql, pgConn))
{
    await cmd.ExecuteNonQueryAsync();
}
Console.WriteLine("  -> Tablas truncadas exitosamente para carga limpia.");

Console.WriteLine("\n[2/4] Migrando tablas de datos de producción...");

// 1. IdentificationTypes
await MigrateTableAsync(sqlConn, pgConn, "IdentificationTypes",
    "SELECT Id, Name FROM IdentificationTypes",
    "INSERT INTO \"IdentificationTypes\" (\"Id\", \"Name\") VALUES (@Id, @Name)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("Name", reader.GetString(1))
    ]);

// 2. Brands
await MigrateTableAsync(sqlConn, pgConn, "Brands",
    "SELECT Id, Name FROM Brands",
    "INSERT INTO \"Brands\" (\"Id\", \"Name\") VALUES (@Id, @Name)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("Name", reader.GetString(1))
    ]);

// 3. ItemTypes
await MigrateTableAsync(sqlConn, pgConn, "ItemTypes",
    "SELECT Id, Name FROM ItemTypes",
    "INSERT INTO \"ItemTypes\" (\"Id\", \"Name\") VALUES (@Id, @Name)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("Name", reader.GetString(1))
    ]);

// 4. Persons
bool personsHasAudit = await ColumnExistsAsync(sqlConn, "Persons", "CreatedAt");
string personsSelectSql = personsHasAudit
    ? "SELECT Id, IdentificationTypeId, IdentificationNumber, FullName, CellPhone, Email, PersonType, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted FROM Persons"
    : "SELECT Id, IdentificationTypeId, IdentificationNumber, FullName, CellPhone, Email, PersonType, GETUTCDATE() AS CreatedAt, NULL AS CreatedBy, NULL AS LastModifiedAt, NULL AS LastModifiedBy, CAST(0 AS bit) AS IsDeleted FROM Persons";

await MigrateTableAsync(sqlConn, pgConn, "Persons",
    personsSelectSql,
    "INSERT INTO \"Persons\" (\"Id\", \"IdentificationTypeId\", \"IdentificationNumber\", \"FullName\", \"CellPhone\", \"Email\", \"PersonType\", \"CreatedAt\", \"CreatedBy\", \"LastModifiedAt\", \"LastModifiedBy\", \"IsDeleted\") " +
    "VALUES (@Id, @IdentificationTypeId, @IdentificationNumber, @FullName, @CellPhone, @Email, @PersonType, @CreatedAt, @CreatedBy, @LastModifiedAt, @LastModifiedBy, @IsDeleted)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("IdentificationTypeId", reader.GetInt32(1)),
        new NpgsqlParameter("IdentificationNumber", reader.GetString(2)),
        new NpgsqlParameter("FullName", reader.GetString(3)),
        new NpgsqlParameter("CellPhone", reader.IsDBNull(4) ? (object)DBNull.Value : reader.GetString(4)),
        new NpgsqlParameter("Email", reader.IsDBNull(5) ? (object)DBNull.Value : reader.GetString(5)),
        new NpgsqlParameter("PersonType", reader.GetString(6)),
        new NpgsqlParameter("CreatedAt", reader.GetDateTime(7)),
        new NpgsqlParameter("CreatedBy", reader.IsDBNull(8) ? (object)DBNull.Value : reader.GetString(8)),
        new NpgsqlParameter("LastModifiedAt", reader.IsDBNull(9) ? (object)DBNull.Value : reader.GetDateTime(9)),
        new NpgsqlParameter("LastModifiedBy", reader.IsDBNull(10) ? (object)DBNull.Value : reader.GetString(10)),
        new NpgsqlParameter("IsDeleted", reader.GetBoolean(11))
    ]);

// 5. Items
bool itemsHasAudit = await ColumnExistsAsync(sqlConn, "Items", "CreatedAt");
string itemsSelectSql = itemsHasAudit
    ? "SELECT Id, Reference, CostValue, DateOfEntry, DateOfSale, SaleValue, Description, Active, ItemTypeId, ProviderId, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted FROM Items"
    : "SELECT Id, Reference, CostValue, DateOfEntry, DateOfSale, SaleValue, Description, Active, ItemTypeId, ProviderId, DateOfEntry AS CreatedAt, NULL AS CreatedBy, NULL AS LastModifiedAt, NULL AS LastModifiedBy, CAST(0 AS bit) AS IsDeleted FROM Items";

await MigrateTableAsync(sqlConn, pgConn, "Items",
    itemsSelectSql,
    "INSERT INTO \"Items\" (\"Id\", \"Reference\", \"CostValue\", \"DateOfEntry\", \"DateOfSale\", \"SaleValue\", \"Description\", \"Active\", \"ItemTypeId\", \"ProviderId\", \"CreatedAt\", \"CreatedBy\", \"LastModifiedAt\", \"LastModifiedBy\", \"IsDeleted\") " +
    "VALUES (@Id, @Reference, @CostValue, @DateOfEntry, @DateOfSale, @SaleValue, @Description, @Active, @ItemTypeId, @ProviderId, @CreatedAt, @CreatedBy, @LastModifiedAt, @LastModifiedBy, @IsDeleted)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("Reference", reader.GetInt32(1)),
        new NpgsqlParameter("CostValue", reader.GetInt32(2)),
        new NpgsqlParameter("DateOfEntry", reader.GetDateTime(3)),
        new NpgsqlParameter("DateOfSale", reader.IsDBNull(4) ? (object)DBNull.Value : reader.GetDateTime(4)),
        new NpgsqlParameter("SaleValue", reader.IsDBNull(5) ? (object)DBNull.Value : reader.GetInt32(5)),
        new NpgsqlParameter("Description", reader.GetString(6)),
        new NpgsqlParameter("Active", reader.GetBoolean(7)),
        new NpgsqlParameter("ItemTypeId", reader.GetInt32(8)),
        new NpgsqlParameter("ProviderId", reader.IsDBNull(9) ? (object)DBNull.Value : reader.GetInt32(9)),
        new NpgsqlParameter("CreatedAt", reader.GetDateTime(10)),
        new NpgsqlParameter("CreatedBy", reader.IsDBNull(11) ? (object)DBNull.Value : reader.GetString(11)),
        new NpgsqlParameter("LastModifiedAt", reader.IsDBNull(12) ? (object)DBNull.Value : reader.GetDateTime(12)),
        new NpgsqlParameter("LastModifiedBy", reader.IsDBNull(13) ? (object)DBNull.Value : reader.GetString(13)),
        new NpgsqlParameter("IsDeleted", reader.GetBoolean(14))
    ]);

// 6. Jewelry
await MigrateTableAsync(sqlConn, pgConn, "Jewelry",
    "SELECT Id, ItemId, Weight, GramValue, Length FROM Jewelry",
    "INSERT INTO \"Jewelry\" (\"Id\", \"ItemId\", \"Weight\", \"GramValue\", \"Length\") VALUES (@Id, @ItemId, @Weight, @GramValue, @Length)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("ItemId", reader.GetInt32(1)),
        new NpgsqlParameter("Weight", reader.GetDouble(2)),
        new NpgsqlParameter("GramValue", reader.GetInt32(3)),
        new NpgsqlParameter("Length", reader.IsDBNull(4) ? (object)DBNull.Value : reader.GetInt32(4))
    ]);

// 7. Watches
await MigrateTableAsync(sqlConn, pgConn, "Watches",
    "SELECT Id, ItemId, GenderType, CaseNumber, BrandId FROM Watches",
    "INSERT INTO \"Watches\" (\"Id\", \"ItemId\", \"GenderType\", \"CaseNumber\", \"BrandId\") VALUES (@Id, @ItemId, @GenderType, @CaseNumber, @BrandId)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("ItemId", reader.GetInt32(1)),
        new NpgsqlParameter("GenderType", reader.IsDBNull(2) ? "" : reader.GetString(2)),
        new NpgsqlParameter("CaseNumber", reader.IsDBNull(3) ? "" : reader.GetString(3)),
        new NpgsqlParameter("BrandId", reader.GetInt32(4))
    ]);

// 8. DailyRecords
bool dailyHasAudit = await ColumnExistsAsync(sqlConn, "DailyRecords", "CreatedAt");
string dailySelectSql = dailyHasAudit
    ? "SELECT Id, Date, Number, Reference, Description, Value, RecordType, PaymentMethod, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted FROM DailyRecords"
    : "SELECT Id, Date, Number, Reference, Description, Value, RecordType, PaymentMethod, Date AS CreatedAt, NULL AS CreatedBy, NULL AS LastModifiedAt, NULL AS LastModifiedBy, CAST(0 AS bit) AS IsDeleted FROM DailyRecords";

await MigrateTableAsync(sqlConn, pgConn, "DailyRecords",
    dailySelectSql,
    "INSERT INTO \"DailyRecords\" (\"Id\", \"Date\", \"Number\", \"Reference\", \"Description\", \"Value\", \"RecordType\", \"PaymentMethod\", \"CreatedAt\", \"CreatedBy\", \"LastModifiedAt\", \"LastModifiedBy\", \"IsDeleted\") " +
    "VALUES (@Id, @Date, @Number, @Reference, @Description, @Value, @RecordType, @PaymentMethod, @CreatedAt, @CreatedBy, @LastModifiedAt, @LastModifiedBy, @IsDeleted)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("Date", reader.GetDateTime(1)),
        new NpgsqlParameter("Number", reader.IsDBNull(2) ? "" : reader.GetString(2)),
        new NpgsqlParameter("Reference", reader.IsDBNull(3) ? (object)DBNull.Value : reader.GetInt32(3)),
        new NpgsqlParameter("Description", reader.GetString(4)),
        new NpgsqlParameter("Value", reader.GetInt32(5)),
        new NpgsqlParameter("RecordType", reader.GetString(6)),
        new NpgsqlParameter("PaymentMethod", reader.GetString(7)),
        new NpgsqlParameter("CreatedAt", reader.GetDateTime(8)),
        new NpgsqlParameter("CreatedBy", reader.IsDBNull(9) ? (object)DBNull.Value : reader.GetString(9)),
        new NpgsqlParameter("LastModifiedAt", reader.IsDBNull(10) ? (object)DBNull.Value : reader.GetDateTime(10)),
        new NpgsqlParameter("LastModifiedBy", reader.IsDBNull(11) ? (object)DBNull.Value : reader.GetString(11)),
        new NpgsqlParameter("IsDeleted", reader.GetBoolean(12))
    ]);

// 9. Contracts
bool contractsHasAudit = await ColumnExistsAsync(sqlConn, "Contracts", "CreatedAt");
string contractsSelectSql = contractsHasAudit
    ? "SELECT Id, Number, Reference, Date, DeliveryDate, Value, State, Balance, Weight, Description, PersonId, ProviderId, FatherId, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted FROM Contracts"
    : "SELECT Id, Number, Reference, Date, DeliveryDate, Value, State, Balance, Weight, Description, PersonId, NULL AS ProviderId, NULL AS FatherId, Date AS CreatedAt, NULL AS CreatedBy, NULL AS LastModifiedAt, NULL AS LastModifiedBy, CAST(0 AS bit) AS IsDeleted FROM Contracts";

await MigrateTableAsync(sqlConn, pgConn, "Contracts",
    contractsSelectSql,
    "INSERT INTO \"Contracts\" (\"Id\", \"Number\", \"Reference\", \"Date\", \"DeliveryDate\", \"Value\", \"State\", \"Balance\", \"Weight\", \"Description\", \"PersonId\", \"ProviderId\", \"FatherId\", \"CreatedAt\", \"CreatedBy\", \"LastModifiedAt\", \"LastModifiedBy\", \"IsDeleted\") " +
    "VALUES (@Id, @Number, @Reference, @Date, @DeliveryDate, @Value, @State, @Balance, @Weight, @Description, @PersonId, @ProviderId, @FatherId, @CreatedAt, @CreatedBy, @LastModifiedAt, @LastModifiedBy, @IsDeleted)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("Number", reader.GetInt32(1)),
        new NpgsqlParameter("Reference", reader.IsDBNull(2) ? (object)DBNull.Value : reader.GetInt32(2)),
        new NpgsqlParameter("Date", reader.GetDateTime(3)),
        new NpgsqlParameter("DeliveryDate", reader.GetDateTime(4)),
        new NpgsqlParameter("Value", reader.GetInt32(5)),
        new NpgsqlParameter("State", reader.GetString(6)),
        new NpgsqlParameter("Balance", reader.GetInt32(7)),
        new NpgsqlParameter("Weight", reader.GetDouble(8)),
        new NpgsqlParameter("Description", reader.GetString(9)),
        new NpgsqlParameter("PersonId", reader.GetInt32(10)),
        new NpgsqlParameter("ProviderId", reader.IsDBNull(11) ? (object)DBNull.Value : reader.GetInt32(11)),
        new NpgsqlParameter("FatherId", reader.IsDBNull(12) ? (object)DBNull.Value : reader.GetInt32(12)),
        new NpgsqlParameter("CreatedAt", reader.GetDateTime(13)),
        new NpgsqlParameter("CreatedBy", reader.IsDBNull(14) ? (object)DBNull.Value : reader.GetString(14)),
        new NpgsqlParameter("LastModifiedAt", reader.IsDBNull(15) ? (object)DBNull.Value : reader.GetDateTime(15)),
        new NpgsqlParameter("LastModifiedBy", reader.IsDBNull(16) ? (object)DBNull.Value : reader.GetString(16)),
        new NpgsqlParameter("IsDeleted", reader.GetBoolean(17))
    ]);

// 10. Payments
bool paymentsHasAudit = await ColumnExistsAsync(sqlConn, "Payments", "CreatedAt");
string paymentsSelectSql = paymentsHasAudit
    ? "SELECT Id, Number, Date, Value, EntityId, PaymentType, PaymentMethod, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted FROM Payments"
    : "SELECT Id, Number, Date, Value, EntityId, PaymentType, PaymentMethod, Date AS CreatedAt, NULL AS CreatedBy, NULL AS LastModifiedAt, NULL AS LastModifiedBy, CAST(0 AS bit) AS IsDeleted FROM Payments";

await MigrateTableAsync(sqlConn, pgConn, "Payments",
    paymentsSelectSql,
    "INSERT INTO \"Payments\" (\"Id\", \"Number\", \"Date\", \"Value\", \"EntityId\", \"PaymentType\", \"PaymentMethod\", \"CreatedAt\", \"CreatedBy\", \"LastModifiedAt\", \"LastModifiedBy\", \"IsDeleted\") " +
    "VALUES (@Id, @Number, @Date, @Value, @EntityId, @PaymentType, @PaymentMethod, @CreatedAt, @CreatedBy, @LastModifiedAt, @LastModifiedBy, @IsDeleted)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("Number", reader.GetString(1)),
        new NpgsqlParameter("Date", reader.GetDateTime(2)),
        new NpgsqlParameter("Value", reader.GetInt32(3)),
        new NpgsqlParameter("EntityId", reader.GetInt32(4)),
        new NpgsqlParameter("PaymentType", reader.GetString(5)),
        new NpgsqlParameter("PaymentMethod", reader.GetString(6)),
        new NpgsqlParameter("CreatedAt", reader.GetDateTime(7)),
        new NpgsqlParameter("CreatedBy", reader.IsDBNull(8) ? (object)DBNull.Value : reader.GetString(8)),
        new NpgsqlParameter("LastModifiedAt", reader.IsDBNull(9) ? (object)DBNull.Value : reader.GetDateTime(9)),
        new NpgsqlParameter("LastModifiedBy", reader.IsDBNull(10) ? (object)DBNull.Value : reader.GetString(10)),
        new NpgsqlParameter("IsDeleted", reader.GetBoolean(11))
    ]);

// 11. Invoices
bool invoicesHasExtra = await ColumnExistsAsync(sqlConn, "Invoices", "PaymentLimitDate");
string invoicesSelectSql = invoicesHasExtra
    ? "SELECT Id, Number, Date, PaymentLimitDate, Value, CreditValue, BalanceValue, ProviderId, Description, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted FROM Invoices"
    : "SELECT Id, Number, Date, Date AS PaymentLimitDate, Value, 0 AS CreditValue, 0 AS BalanceValue, 0 AS ProviderId, '' AS Description, Date AS CreatedAt, NULL AS CreatedBy, NULL AS LastModifiedAt, NULL AS LastModifiedBy, CAST(0 AS bit) AS IsDeleted FROM Invoices";

await MigrateTableAsync(sqlConn, pgConn, "Invoices",
    invoicesSelectSql,
    "INSERT INTO \"Invoices\" (\"Id\", \"Number\", \"Date\", \"PaymentLimitDate\", \"Value\", \"CreditValue\", \"BalanceValue\", \"ProviderId\", \"Description\", \"CreatedAt\", \"CreatedBy\", \"LastModifiedAt\", \"LastModifiedBy\", \"IsDeleted\") " +
    "VALUES (@Id, @Number, @Date, @PaymentLimitDate, @Value, @CreditValue, @BalanceValue, @ProviderId, @Description, @CreatedAt, @CreatedBy, @LastModifiedAt, @LastModifiedBy, @IsDeleted)",
    reader => [
        new NpgsqlParameter("Id", reader.GetInt32(0)),
        new NpgsqlParameter("Number", reader.GetString(1)),
        new NpgsqlParameter("Date", reader.GetDateTime(2)),
        new NpgsqlParameter("PaymentLimitDate", reader.GetDateTime(3)),
        new NpgsqlParameter("Value", reader.GetInt32(4)),
        new NpgsqlParameter("CreditValue", reader.GetInt32(5)),
        new NpgsqlParameter("BalanceValue", reader.GetInt32(6)),
        new NpgsqlParameter("ProviderId", reader.GetInt32(7)),
        new NpgsqlParameter("Description", reader.GetString(8)),
        new NpgsqlParameter("CreatedAt", reader.GetDateTime(9)),
        new NpgsqlParameter("CreatedBy", reader.IsDBNull(10) ? (object)DBNull.Value : reader.GetString(10)),
        new NpgsqlParameter("LastModifiedAt", reader.IsDBNull(11) ? (object)DBNull.Value : reader.GetDateTime(11)),
        new NpgsqlParameter("LastModifiedBy", reader.IsDBNull(12) ? (object)DBNull.Value : reader.GetString(12)),
        new NpgsqlParameter("IsDeleted", reader.GetBoolean(13))
    ]);

// 12. AspNetUsers
bool usersHasAudit = await ColumnExistsAsync(sqlConn, "AspNetUsers", "CreatedAt");
string usersSelectSql = usersHasAudit
    ? "SELECT Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, CreatedAt, LastLoginAt, IsActive FROM AspNetUsers"
    : "SELECT Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, GETUTCDATE() AS CreatedAt, NULL AS LastLoginAt, CAST(1 AS bit) AS IsActive FROM AspNetUsers";

await MigrateTableAsync(sqlConn, pgConn, "AspNetUsers",
    usersSelectSql,
    "INSERT INTO \"AspNetUsers\" (\"Id\", \"UserName\", \"NormalizedUserName\", \"Email\", \"NormalizedEmail\", \"EmailConfirmed\", \"PasswordHash\", \"SecurityStamp\", \"ConcurrencyStamp\", \"PhoneNumber\", \"PhoneNumberConfirmed\", \"TwoFactorEnabled\", \"LockoutEnd\", \"LockoutEnabled\", \"AccessFailedCount\", \"CreatedAt\", \"LastLoginAt\", \"IsActive\") " +
    "VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed, @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @PhoneNumber, @PhoneNumberConfirmed, @TwoFactorEnabled, @LockoutEnd, @LockoutEnabled, @AccessFailedCount, @CreatedAt, @LastLoginAt, @IsActive) " +
    "ON CONFLICT (\"Id\") DO UPDATE SET " +
    "\"UserName\" = EXCLUDED.\"UserName\", " +
    "\"NormalizedUserName\" = EXCLUDED.\"NormalizedUserName\", " +
    "\"Email\" = EXCLUDED.\"Email\", " +
    "\"NormalizedEmail\" = EXCLUDED.\"NormalizedEmail\", " +
    "\"PasswordHash\" = EXCLUDED.\"PasswordHash\", " +
    "\"SecurityStamp\" = EXCLUDED.\"SecurityStamp\", " +
    "\"ConcurrencyStamp\" = EXCLUDED.\"ConcurrencyStamp\", " +
    "\"IsActive\" = EXCLUDED.\"IsActive\"",
    reader => [
        new NpgsqlParameter("Id", reader.GetString(0)),
        new NpgsqlParameter("UserName", reader.IsDBNull(1) ? (object)DBNull.Value : reader.GetString(1)),
        new NpgsqlParameter("NormalizedUserName", reader.IsDBNull(2) ? (object)DBNull.Value : reader.GetString(2)),
        new NpgsqlParameter("Email", reader.IsDBNull(3) ? (object)DBNull.Value : reader.GetString(3)),
        new NpgsqlParameter("NormalizedEmail", reader.IsDBNull(4) ? (object)DBNull.Value : reader.GetString(4)),
        new NpgsqlParameter("EmailConfirmed", reader.GetBoolean(5)),
        new NpgsqlParameter("PasswordHash", reader.IsDBNull(6) ? (object)DBNull.Value : reader.GetString(6)),
        new NpgsqlParameter("SecurityStamp", reader.IsDBNull(7) ? (object)DBNull.Value : reader.GetString(7)),
        new NpgsqlParameter("ConcurrencyStamp", reader.IsDBNull(8) ? (object)DBNull.Value : reader.GetString(8)),
        new NpgsqlParameter("PhoneNumber", reader.IsDBNull(9) ? (object)DBNull.Value : reader.GetString(9)),
        new NpgsqlParameter("PhoneNumberConfirmed", reader.GetBoolean(10)),
        new NpgsqlParameter("TwoFactorEnabled", reader.GetBoolean(11)),
        new NpgsqlParameter("LockoutEnd", reader.IsDBNull(12) ? (object)DBNull.Value : reader.GetFieldValue<DateTimeOffset>(12)),
        new NpgsqlParameter("LockoutEnabled", reader.GetBoolean(13)),
        new NpgsqlParameter("AccessFailedCount", reader.GetInt32(14)),
        new NpgsqlParameter("CreatedAt", reader.GetDateTime(15)),
        new NpgsqlParameter("LastLoginAt", reader.IsDBNull(16) ? (object)DBNull.Value : reader.GetDateTime(16)),
        new NpgsqlParameter("IsActive", reader.GetBoolean(17))
    ]);

// 13. Garantizar existencia del usuario administrador de soporte (walvarez@ingnova.team)
var ensureAdminUserSql = @"
    INSERT INTO ""AspNetUsers"" (
        ""Id"", ""UserName"", ""NormalizedUserName"", ""Email"", ""NormalizedEmail"", 
        ""EmailConfirmed"", ""PasswordHash"", ""SecurityStamp"", ""ConcurrencyStamp"", 
        ""PhoneNumberConfirmed"", ""TwoFactorEnabled"", ""LockoutEnabled"", ""AccessFailedCount"", 
        ""CreatedAt"", ""IsActive"")
    VALUES (
        '4e36e16a-2dad-45ac-aa29-5b005ed98f10', 'walvarez@ingnova.team', 'WALVAREZ@INGNOVA.TEAM', 
        'walvarez@ingnova.team', 'WALVAREZ@INGNOVA.TEAM', true, 
        'AQAAAAIAAYagAAAAELyX1ptMnY+wJhIK3NO7hkuW7C73++kiRjTGU8jNhiCNJkO0Ma8C2oNM6TRvxuypFA==', 
        'RML5FHTMVQA52BNOYK3Z6FU3IE3QHAOY', 'a3378456-32d4-4495-935f-c04825acd9d7', 
        false, false, true, 0, NOW(), true)
    ON CONFLICT (""Id"") DO UPDATE SET 
        ""PasswordHash"" = EXCLUDED.""PasswordHash"",
        ""IsActive"" = true;
";
using (var cmd = new NpgsqlCommand(ensureAdminUserSql, pgConn))
{
    await cmd.ExecuteNonQueryAsync();
}
Console.WriteLine("  -> Usuario administrador de soporte (walvarez@ingnova.team) asegurado.");

// 14. AspNetUserRoles (si existen en SQL Server)
if (await TableHasRowsAsync(sqlConn, "AspNetUserRoles"))
{
    await MigrateTableAsync(sqlConn, pgConn, "AspNetUserRoles",
        "SELECT u.NormalizedUserName, r.NormalizedName " +
        "FROM AspNetUserRoles ur " +
        "JOIN AspNetUsers u ON ur.UserId = u.Id " +
        "JOIN AspNetRoles r ON ur.RoleId = r.Id",
        "INSERT INTO \"AspNetUserRoles\" (\"UserId\", \"RoleId\") " +
        "SELECT u.\"Id\", r.\"Id\" FROM \"AspNetUsers\" u CROSS JOIN \"AspNetRoles\" r " +
        "WHERE u.\"NormalizedUserName\" = @NormalizedUserName AND r.\"NormalizedName\" = @NormalizedRoleName " +
        "ON CONFLICT (\"UserId\", \"RoleId\") DO NOTHING",
        reader => [
            new NpgsqlParameter("NormalizedUserName", reader.GetString(0)),
            new NpgsqlParameter("NormalizedRoleName", reader.GetString(1))
        ]);
}

// 15. Asignar roles administrativos a los usuarios del sistema
var assignRolesSql = @"
    INSERT INTO ""AspNetUserRoles"" (""UserId"", ""RoleId"")
    SELECT u.""Id"", r.""Id"" 
    FROM ""AspNetUsers"" u 
    CROSS JOIN ""AspNetRoles"" r 
    WHERE u.""NormalizedUserName"" IN ('ADMIN@JOYERIAPOMONA.COM', 'WALVAREZ@INGNOVA.TEAM') 
      AND r.""NormalizedName"" IN ('ADMIN', 'ADMINISTRADOR')
    ON CONFLICT (""UserId"", ""RoleId"") DO NOTHING;
";
using (var cmd = new NpgsqlCommand(assignRolesSql, pgConn))
{
    var rolesAssigned = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"  -> Roles administrativos asociados ({rolesAssigned} asignaciones).");
}

Console.WriteLine("\n[3/4] Sincronizando secuencias de PostgreSQL al último ID...");
string[] serialTables = [
    "IdentificationTypes", "Brands", "ItemTypes", "Persons", 
    "Items", "Jewelry", "Watches", "DailyRecords", 
    "Contracts", "Payments", "Invoices"
];
foreach (var table in serialTables)
{
    var sql = $"SELECT setval(pg_get_serial_sequence('\"{table}\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"{table}\"), 1), (SELECT COUNT(*) > 0 FROM \"{table}\"));";
    using var cmd = new NpgsqlCommand(sql, pgConn);
    var val = await cmd.ExecuteScalarAsync();
    Console.WriteLine($"  -> Secuencia de \"{table}\": valor ajustado a {val}");
}

Console.WriteLine("\n[4/4] Validando paridad de registros...");
Console.WriteLine("------------------------------------------------------------------");
Console.WriteLine(string.Format("{0,-20} | {1,12} | {2,12} | {3,-10}", "Tabla", "SQL Server", "PostgreSQL", "Estado"));
Console.WriteLine("------------------------------------------------------------------");

string[] businessTables = [
    "IdentificationTypes", "Brands", "ItemTypes", "Persons", 
    "Items", "Jewelry", "Watches", "DailyRecords", 
    "Contracts", "Payments", "Invoices"
];

bool allMatch = true;
foreach (var table in businessTables)
{
    using var sqlCountCmd = new SqlCommand($"SELECT COUNT(*) FROM [{table}]", sqlConn);
    using var pgCountCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{table}\"", pgConn);

    var sqlCount = (int)(await sqlCountCmd.ExecuteScalarAsync() ?? 0);
    var pgCount = Convert.ToInt32(await pgCountCmd.ExecuteScalarAsync() ?? 0);
    var match = sqlCount == pgCount;
    if (!match) allMatch = false;

    Console.WriteLine(string.Format("{0,-20} | {1,12} | {2,12} | {3,-10}",
        table, sqlCount, pgCount, match ? "CORRECTO" : "DESCUADRE"));
}

Console.WriteLine("------------------------------------------------------------------");

// Comprobación informativa de seguridad / usuarios
using (var pgUsersCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"AspNetUsers\"", pgConn))
using (var pgRolesCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"AspNetUserRoles\"", pgConn))
{
    var uCount = Convert.ToInt32(await pgUsersCmd.ExecuteScalarAsync() ?? 0);
    var rCount = Convert.ToInt32(await pgRolesCmd.ExecuteScalarAsync() ?? 0);
    Console.WriteLine($"Seguridad: {uCount} usuarios activos con {rCount} roles administrativos asignados.");
}
Console.WriteLine("------------------------------------------------------------------");

if (allMatch)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("¡MIGRACIÓN COMPLETADA CON ÉXITO Y PARIDAD DEL 100%!");
    Console.ResetColor();
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Advertencia: Existen discrepancias en los conteos de registros de negocio.");
    Console.ResetColor();
}

static async Task MigrateTableAsync(
    SqlConnection sqlConn, 
    NpgsqlConnection pgConn, 
    string tableName, 
    string selectSql, 
    string insertSql, 
    Func<SqlDataReader, NpgsqlParameter[]> paramFactory)
{
    using var sqlCmd = new SqlCommand(selectSql, sqlConn);
    using var reader = await sqlCmd.ExecuteReaderAsync();

    using var tx = await pgConn.BeginTransactionAsync();
    var count = 0;
    try
    {
        while (await reader.ReadAsync())
        {
            using var pgCmd = new NpgsqlCommand(insertSql, pgConn, tx);
            pgCmd.Parameters.AddRange(paramFactory(reader));
            await pgCmd.ExecuteNonQueryAsync();
            count++;
        }
        await tx.CommitAsync();
        Console.WriteLine($"  -> \"{tableName}\": {count} registros transferidos.");
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [ERROR] Fallo al migrar \"{tableName}\": {ex.Message}");
        Console.ResetColor();
        throw;
    }
}

static async Task<bool> ColumnExistsAsync(SqlConnection conn, string table, string column)
{
    using var cmd = new SqlCommand(
        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @Table AND COLUMN_NAME = @Col", conn);
    cmd.Parameters.AddWithValue("@Table", table);
    cmd.Parameters.AddWithValue("@Col", column);
    return (int)(await cmd.ExecuteScalarAsync() ?? 0) > 0;
}

static async Task<bool> TableHasRowsAsync(SqlConnection conn, string table)
{
    using var cmd = new SqlCommand($"SELECT COUNT(*) FROM [{table}]", conn);
    return (int)(await cmd.ExecuteScalarAsync() ?? 0) > 0;
}
