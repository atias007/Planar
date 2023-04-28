EXECUTE sp_rename N'dbo.Groups.Reference1', N'Tmp_AdditionalField1', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Groups.Reference2', N'Tmp_AdditionalField2_1', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Groups.Reference3', N'Tmp_AdditionalField3_2', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Groups.Reference4', N'Tmp_AdditionalField4_3', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Groups.Reference5', N'Tmp_AdditionalField5_4', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Groups.Tmp_AdditionalField1', N'AdditionalField1', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Groups.Tmp_AdditionalField2_1', N'AdditionalField2', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Groups.Tmp_AdditionalField3_2', N'AdditionalField3', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Groups.Tmp_AdditionalField4_3', N'AdditionalField4', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Groups.Tmp_AdditionalField5_4', N'AdditionalField5', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Users.Reference1', N'Tmp_AdditionalField1_5', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Users.Reference2', N'Tmp_AdditionalField2_6', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Users.Reference3', N'Tmp_AdditionalField3_7', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Users.Reference4', N'Tmp_AdditionalField4_8', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Users.Reference5', N'Tmp_AdditionalField5_9', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Users.Tmp_AdditionalField1_5', N'AdditionalField1', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Users.Tmp_AdditionalField2_6', N'AdditionalField2', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Users.Tmp_AdditionalField3_7', N'AdditionalField3', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Users.Tmp_AdditionalField4_8', N'AdditionalField4', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.Users.Tmp_AdditionalField5_9', N'AdditionalField5', 'COLUMN' 
