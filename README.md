# OrgillUtility
Utility for creating purchase order for [Orgill](https://Orgill.com), from RockSolid Roman (They don't even have a sales page anymore).

> This project was created for the very specific application of creating a web order, as the ftp order system was likely to fail.
> It was also designed around the limitations of the out-dated inventory management software, and the need to minimize order sizes due to supply chain issues

## How To Use
1. Login to your Orgill account
2. Set the maximum number of lines allowed in the purchase order
3. Select the excel file purchase order exported from RockSolid Roman
4. Wait for completion; More data is available in the dropdown
5. Upload the exported csv to orgill.com

* The program will also export a list of all the products that were out in the warehouse.
* Occasionally, the progrom exports a list of lines that were unreadable, and indicate errors in the pos that need to be corrected.

## Installation
Installation file for the current build can be found [here](Installer%20Project/Installer%20Project-SetupFiles/)
