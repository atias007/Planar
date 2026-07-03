ALTER TABLE [dbo].[GlobalConfig] ADD [IsSecret] [bit] NOT NULL DEFAULT(0);
ALTER TABLE [dbo].[GlobalConfig] ADD [SecretKey] [varchar](50) NULL