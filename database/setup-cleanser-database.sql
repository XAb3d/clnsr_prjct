IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [MustChangePassword] bit NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE TABLE [BusinessesData] (
        [Id] int NOT NULL IDENTITY,
        [CurrenVersion] int NOT NULL,
        [SubscriberCode] nvarchar(max) NOT NULL,
        [CreditFacilityAccNum] nvarchar(max) NULL,
        [CustomerID] nvarchar(max) NULL,
        [DisbursementDate] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_BusinessesData] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE TABLE [IndividualsData] (
        [Id] int NOT NULL IDENTITY,
        [CurrenVersion] int NOT NULL,
        [SubscriberCode] nvarchar(max) NOT NULL,
        [CreditFacilityAccNum] nvarchar(max) NULL,
        [CustomerID] nvarchar(max) NULL,
        [DateOfBirth] nvarchar(max) NULL,
        [DisbursementDate] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_IndividualsData] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE TABLE [Settings] (
        [Id] int NOT NULL IDENTITY,
        [Value] nvarchar(max) NULL,
        [DataType] int NOT NULL,
        CONSTRAINT [PK_Settings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250408095937_init4'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250408095937_init4', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250411153447_clean1'
)
BEGIN
    ALTER TABLE [BusinessesData] ADD [DateOfBirth] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250411153447_clean1'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250411153447_clean1', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250416163224_bus1'
)
BEGIN
    CREATE TABLE [BusinessClassNormalizer] (
        [Id] int NOT NULL IDENTITY,
        [ShortValue] nvarchar(max) NULL,
        [LongValue] nvarchar(max) NULL,
        [DataType] int NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        [DateModified] datetime2 NOT NULL,
        CONSTRAINT [PK_BusinessClassNormalizer] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250416163224_bus1'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250416163224_bus1', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716112156_sprint5_ref_id_fields'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [DriverLicNum] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716112156_sprint5_ref_id_fields'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [EzwichNum] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716112156_sprint5_ref_id_fields'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [LastUpdatedDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716112156_sprint5_ref_id_fields'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [NatIDNum] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716112156_sprint5_ref_id_fields'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [OtherIDNum] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716112156_sprint5_ref_id_fields'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [PassportNum] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716112156_sprint5_ref_id_fields'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [SSNum] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716112156_sprint5_ref_id_fields'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [VotersIDNum] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716112156_sprint5_ref_id_fields'
)
BEGIN
    ALTER TABLE [BusinessesData] ADD [LastUpdatedDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716112156_sprint5_ref_id_fields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716112156_sprint5_ref_id_fields', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716120255_sprint5_ref_id_fields_v2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716120255_sprint5_ref_id_fields_v2', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719143223_sprint10_ref_name_fields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719143223_sprint10_ref_name_fields', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728005909_AddSubscriberProfile'
)
BEGIN
    CREATE TABLE [SubscriberProfiles] (
        [Id] int NOT NULL IDENTITY,
        [SubscriberCode] nvarchar(max) NOT NULL,
        [SubscriberName] nvarchar(max) NOT NULL,
        [InstitutionType] nvarchar(max) NOT NULL,
        [LastUpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_SubscriberProfiles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728005909_AddSubscriberProfile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728005909_AddSubscriberProfile', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729025215_AddIndividualRefMiddleNames'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [MiddleNames] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729025215_AddIndividualRefMiddleNames'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729025215_AddIndividualRefMiddleNames', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730040711_AddFacilityStatusAndBusinessIdentity'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [FacilityStatusCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730040711_AddFacilityStatusAndBusinessIdentity'
)
BEGIN
    ALTER TABLE [BusinessesData] ADD [Businessname] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730040711_AddFacilityStatusAndBusinessIdentity'
)
BEGIN
    ALTER TABLE [BusinessesData] ADD [Busregnum] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730040711_AddFacilityStatusAndBusinessIdentity'
)
BEGIN
    ALTER TABLE [BusinessesData] ADD [FacilityStatusCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730040711_AddFacilityStatusAndBusinessIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730040711_AddFacilityStatusAndBusinessIdentity', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802024214_AddBusinessRefTinumAndBusregnumFlag'
)
BEGIN
    ALTER TABLE [BusinessesData] ADD [Tinum] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802024214_AddBusinessRefTinumAndBusregnumFlag'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802024214_AddBusinessRefTinumAndBusregnumFlag', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214212_AddUnloadableLogTables'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SubscriberProfiles]') AND [c].[name] = N'SubscriberCode');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [SubscriberProfiles] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [SubscriberProfiles] ALTER COLUMN [SubscriberCode] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214212_AddUnloadableLogTables'
)
BEGIN
    CREATE TABLE [UnloadableLogHeaders] (
        [Id] int NOT NULL IDENTITY,
        [SubscriberProfileId] int NOT NULL,
        [Associate] nvarchar(max) NOT NULL,
        [Filename] nvarchar(max) NOT NULL,
        [NumberOfRecords] int NOT NULL,
        [ReportingPeriod] nvarchar(max) NOT NULL,
        [ReportingYear] nvarchar(max) NOT NULL,
        [DataType] nvarchar(max) NOT NULL,
        [Months] nvarchar(max) NOT NULL,
        [LogYear] nvarchar(max) NOT NULL,
        [DateEmailed] datetime2 NULL,
        [DateFixed] datetime2 NULL,
        [Comments] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_UnloadableLogHeaders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnloadableLogHeaders_SubscriberProfiles_SubscriberProfileId] FOREIGN KEY ([SubscriberProfileId]) REFERENCES [SubscriberProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214212_AddUnloadableLogTables'
)
BEGIN
    CREATE TABLE [UnloadableLogCategoryDetails] (
        [Id] int NOT NULL IDENTITY,
        [UnloadableLogHeaderId] int NOT NULL,
        [TopLevelCategory] nvarchar(max) NOT NULL,
        [SubCategory] nvarchar(max) NOT NULL,
        [DescriptionOfErrors] nvarchar(max) NOT NULL,
        [VolumeAffected] int NOT NULL,
        [Percentage] float NOT NULL,
        CONSTRAINT [PK_UnloadableLogCategoryDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnloadableLogCategoryDetails_UnloadableLogHeaders_UnloadableLogHeaderId] FOREIGN KEY ([UnloadableLogHeaderId]) REFERENCES [UnloadableLogHeaders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214212_AddUnloadableLogTables'
)
BEGIN
    CREATE TABLE [UnloadableLogMessageDetails] (
        [Id] int NOT NULL IDENTITY,
        [UnloadableLogHeaderId] int NOT NULL,
        [ErrorMessage] nvarchar(max) NOT NULL,
        [Count] int NOT NULL,
        [Percentage] float NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_UnloadableLogMessageDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnloadableLogMessageDetails_UnloadableLogHeaders_UnloadableLogHeaderId] FOREIGN KEY ([UnloadableLogHeaderId]) REFERENCES [UnloadableLogHeaders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214212_AddUnloadableLogTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SubscriberProfiles_SubscriberCode] ON [SubscriberProfiles] ([SubscriberCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214212_AddUnloadableLogTables'
)
BEGIN
    CREATE INDEX [IX_UnloadableLogCategoryDetails_UnloadableLogHeaderId] ON [UnloadableLogCategoryDetails] ([UnloadableLogHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214212_AddUnloadableLogTables'
)
BEGIN
    CREATE INDEX [IX_UnloadableLogHeaders_SubscriberProfileId] ON [UnloadableLogHeaders] ([SubscriberProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214212_AddUnloadableLogTables'
)
BEGIN
    CREATE INDEX [IX_UnloadableLogMessageDetails_UnloadableLogHeaderId] ON [UnloadableLogMessageDetails] ([UnloadableLogHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214212_AddUnloadableLogTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805214212_AddUnloadableLogTables', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809015845_MoveSubscriberShortCodesIntoAppDb'
)
BEGIN
    CREATE TABLE [SubscriberShortCodes] (
        [Id] int NOT NULL IDENTITY,
        [ShortName] nvarchar(max) NOT NULL,
        [SubCategoryCode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_SubscriberShortCodes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809015845_MoveSubscriberShortCodesIntoAppDb'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260809015845_MoveSubscriberShortCodesIntoAppDb', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224704_AddLastConfirmedReportingPeriod'
)
BEGIN
    ALTER TABLE [IndividualsData] ADD [LastConfirmedReportingPeriod] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224704_AddLastConfirmedReportingPeriod'
)
BEGIN
    ALTER TABLE [BusinessesData] ADD [LastConfirmedReportingPeriod] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224704_AddLastConfirmedReportingPeriod'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810224704_AddLastConfirmedReportingPeriod', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [ContactEmail1] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [ContactEmail2] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [ContactEmail3] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [ContactName1] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [ContactName2] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [ContactName3] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [ContactPhone1] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [ContactPhone2] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [ContactPhone3] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [DateCreated] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [DateSubscribe] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [DeactivatedReason] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [DistrictCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [Email] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [EntryDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [Position1] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [Position2] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [Position3] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [PostAdd] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [RegionCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [Remarks] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [SectorCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [SubBoGCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [SubCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [SubName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [SubStatus] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [SubXDSCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [Town] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [TransDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    ALTER TABLE [SubscriberShortCodes] ADD [UserID] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831031311_ExpandSubscriberShortCodes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831031311_ExpandSubscriberShortCodes', N'9.0.7');
END;

COMMIT;
GO

