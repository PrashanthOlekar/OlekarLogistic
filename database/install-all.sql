/* =============================================================================
   ProCargo — install everything in one go.

   In SSMS: open this file, turn on Query > SQLCMD Mode, then press F5.
   From a terminal (run from the database folder):
       sqlcmd -S localhost -E -C -I -i install-all.sql

   On a new server this creates the database, tables and starting data
   (ProCargo.sql), then every stored procedure. On an existing ProCargo
   database, run only the files in procedures/ — they are safe to run again.
   ============================================================================= */

:on error exit

:r ProCargo.sql
:r procedures/01_ReferenceData.sql
:r procedures/02_Accounts.sql
:r procedures/03_Fleet.sql
:r procedures/04_Bookings.sql
:r procedures/05_Trips.sql
:r procedures/06_DocumentsAndMoney.sql
