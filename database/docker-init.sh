#!/bin/bash
# Used by docker-compose: waits for SQL Server, then installs the database.
#   New server:          ProCargo.sql + every stored procedure (install-all.sql)
#   Existing database:   only the stored procedures, which are safe to run again
set -euo pipefail

SQLCMD=(/opt/mssql-tools18/bin/sqlcmd -S "${SQL_HOST:-sql}" -U sa -P "$MSSQL_SA_PASSWORD" -C -I -b)

for _ in $(seq 1 60); do
  "${SQLCMD[@]}" -Q "SELECT 1" >/dev/null 2>&1 && break
  sleep 2
done

cd /database
exists=$("${SQLCMD[@]}" -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('ProCargo') IS NULL THEN 0 ELSE 1 END")

if [ "$(echo "$exists" | tr -d '[:space:]')" = "0" ]; then
  echo "Creating the ProCargo database"
  "${SQLCMD[@]}" -i install-all.sql
else
  echo "ProCargo database exists; updating stored procedures"
  for file in procedures/*.sql; do
    "${SQLCMD[@]}" -i "$file"
  done
fi
echo "Database ready"
