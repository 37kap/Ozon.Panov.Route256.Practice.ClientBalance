using FluentMigrator;

namespace Ozon.Panov.Route256.Practice.ClientBalance.Infrastructure.BalanceDatabaseManagement.Migrations;

[Migration(202503090001)]
public class InitDatabase : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE TYPE operation_status AS ENUM ('pending', 'cancelled', 'completed', 'reject');");
        Execute.Sql("CREATE TYPE operation_type AS ENUM ('top_up', 'withdraw');");

        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS clients (
                client_id BIGINT PRIMARY KEY,
                balance DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                CONSTRAINT chk_balance CHECK (balance >= 0));
        ");

        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS operations (
                operation_id UUID PRIMARY KEY,
                client_id BIGINT NOT NULL REFERENCES clients(client_id),
                amount DECIMAL(18,2) NOT NULL,
                status operation_status NOT NULL DEFAULT 'pending',    
                operation_type operation_type NOT NULL,
                time TIMESTAMP WITH TIME ZONE NOT NULL,
                CONSTRAINT chk_amount_positive CHECK (amount >= 0));
            ");
        Execute.Sql("CREATE INDEX idx_operations_client_id ON operations(client_id);");

        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS operations_log (
                log_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                operation_id UUID NOT NULL,
                operation_type operation_type NOT NULL,
                client_id BIGINT NOT NULL REFERENCES clients(client_id),
                amount DECIMAL(18,2) NOT NULL,
                status operation_status NOT NULL,
                time TIMESTAMP WITH TIME ZONE NOT NULL);
            ");
        Execute.Sql("CREATE INDEX idx_operations_log_operation_id ON operations_log(operation_id);");
        Execute.Sql("CREATE INDEX idx_operations_log_client_id_time ON operations_log(client_id, time);");
    }

    public override void Down()
    {
        Execute.Sql("DROP TYPE IF EXISTS operation_status;");
        Execute.Sql("DROP TYPE IF EXISTS operation_type;");

        Execute.Sql("DROP TABLE IF EXISTS clients;");

        Execute.Sql("DROP INDEX IF EXISTS idx_operations_client_id;");
        Execute.Sql("DROP TABLE IF EXISTS operations;");

        Execute.Sql("DROP INDEX IF EXISTS idx_operations_log_operation_id;");
        Execute.Sql("DROP INDEX IF EXISTS idx_operations_log_client_id_time;");
        Execute.Sql("DROP TABLE IF EXISTS operations_log;");
    }
}
