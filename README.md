📡 Device-Monitor
A Device Monitoring Application using ASP.NET Core MVC & REST APIs




🚀 Overview
Device-Monitor is a web application built using ASP.NET Core MVC with REST API capabilities. It provides a structured way to monitor devices, offering features such as authentication, SNMP-based monitoring, PDF report generation, and email notifications.

🎯 Project Goals
The objective of this project is to build hands-on experience with:
✅ ASP.NET Core MVC - Understanding Model-View-Controller architecture
✅ CRUD Operations - Implementing Create, Read, Update, Delete functionality
✅ Authentication & Authorization - Including:

Traditional Login & Registration

Third-Party Authentication (Google, Microsoft OAuth)
✅ SNMP Integration - For device communication and status monitoring
✅ QuestPDF - Generate dynamic PDF reports
✅ MailKit - Send emails (e.g., password recovery, notifications)

🛠️ Tech Stack
Technology	Description
ASP.NET Core MVC	Backend framework for web app development
Entity Framework Core	ORM for database management
SQL Server / PostgreSQL	Relational database
Google & Microsoft OAuth	Third-party authentication
SNMP	Network device monitoring
QuestPDF	Generate PDF reports dynamically
MailKit	Email notifications
Bootstrap / Tailwind CSS	Frontend UI styling
📸 Screenshots
(Add relevant screenshots of your UI & features here!)

🚀 Getting Started
🔹 Prerequisites
Before running the application, ensure you have:

.NET SDK (7.0 or later)

SQL Server / PostgreSQL installed

Google & Microsoft OAuth credentials

🔹 Installation & Setup
1️⃣ Clone the repository

sh
Sao chép
Chỉnh sửa
git clone https://github.com/your-username/Device-Monitor.git
cd Device-Monitor
2️⃣ Configure the database

Update appsettings.json with your database connection string.

3️⃣ Set up Google & Microsoft Authentication

Obtain Client IDs & Secrets from Google & Microsoft Developer Portals.

Add them to appsettings.json:

json
Sao chép
Chỉnh sửa
"Authentication": {
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "Microsoft": {
    "ClientId": "YOUR_MICROSOFT_CLIENT_ID",
    "ClientSecret": "YOUR_MICROSOFT_CLIENT_SECRET"
  }
}
4️⃣ Run the Application

sh
Sao chép
Chỉnh sửa
dotnet run
