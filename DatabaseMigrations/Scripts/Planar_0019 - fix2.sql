declare @count int
SELECT  @count = count(*)
    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS 
    WHERE CONSTRAINT_NAME='FK_Roles_Roles1'
if( @count > 0)
begin
ALTER TABLE dbo.Roles DROP CONSTRAINT FK_Roles_Roles1
end