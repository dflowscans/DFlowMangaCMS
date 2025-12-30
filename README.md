# DFlowMangaCMS
A custom MangaCMS built on NET.ASP MVC and Razor pages.

## ⚠️ Warning:
- Uploading actual image files as chapter pages does **NOT** work.<br>

Please don't make any issues about that.<br>
I plan to add it in the somewhat near future.

### Note:
- Profile image uploads still work as well as all other image uploads.

#### Note: Only this README will be updated. The others you see in this repo will NOT be updated.

# 🌍 Environment Configuration Guide

This project uses environment variables for secure configuration. This is the recommended way to handle database credentials and other sensitive information without hardcoding them in the source code.

## 🔑 Required Variables

The following environment variables are used in `Program.cs` to build the MySQL connection string:

| Variable | Description | Default Value (if not set) |
|----------|-------------|----------------------------|
| `MYSQL_HOST` | The IP address or hostname of your MySQL server | `YOUR_DATABASE_HOST_IP` |
| `MYSQL_PORT` | The port your MySQL server is listening on | `3306` |
| `MYSQL_DB` | The name of the database for the reader | `YOUR_DATABASE_NAME` |
| `MYSQL_USER` | The database user with read/write permissions | `YOUR_DATABASE_USER` |
| `MYSQL_PASSWORD` | The password for the database user | `YOUR_DATABASE_PASSWORD` |

---

## 🚀 How to Set Variables

### 1. Locally (Development)
For local development, you can set these in your terminal or via your IDE (like Rider or Visual Studio) in the "Run/Debug Configurations" menu.

**PowerShell:**
```powershell
$env:MYSQL_HOST="127.0.0.1"
$env:MYSQL_DB="manga_db"
$env:MYSQL_USER="root"
$env:MYSQL_PASSWORD="your_password"
```

**Bash:**
```bash
export MYSQL_HOST="127.0.0.1"
export MYSQL_DB="manga_db"
export MYSQL_USER="root"
export MYSQL_PASSWORD="your_password"
```

### 2. Docker
If running via Docker, add them to your `docker-compose.yml` or `docker run` command:
```yaml
environment:
  - MYSQL_HOST=db_host
  - MYSQL_DB=manga_db
  - MYSQL_USER=admin
  - MYSQL_PASSWORD=secret_pass
```

---

## ⚠️ Security Note
**Never** commit your actual `.env` files or hardcode real passwords back into `Program.cs`. Always keep these values private to your server environment.


## Compiling the code:
#### Note: This is not neccessary but it is recommended as sometimes I'll probably forget to compile it myself and you'll be using an older version.
#### Note: All commands should be run in the root directory of the project.

To get started run:
```
dotnet publish -c Release -r linux-x64 --self-contained true -o .\publish
```

This will generate the following folders:
- bin
- publish
- wwwroot

## Hosting:

#### Note: This project has only been tested on dedicated VPS running Ubuntu!

Using any software that allows file transfers using FTP<br>
1. Move the contents of `MangaReader/bin/Release/net10.0/linux-x64/` folder into the root folder of your projec on your VPS.<br>
2. Move both folders `wwwroot` and `publish` in the same folder.
