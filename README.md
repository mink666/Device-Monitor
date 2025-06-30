# 📡 Device Monitor

*A real-time, full-stack device monitoring application built with ASP.NET Core.*

## 🚀 Overview

Device Monitor is a web application engineered to provide real-time insights into the health and performance of network devices. It leverages a modern .NET backend, a dynamic JavaScript frontend, and the SNMP protocol for data collection. Key features include a responsive live dashboard, a proactive health analysis engine with configurable thresholds, a dual-channel notification system (in-app and email), and comprehensive PDF reporting.

## ✨ Key Features

  * **Real-time Monitoring Dashboard:** An interactive two-pane UI featuring color-coded device status icons for at-a-glance network health assessment and a detailed view for selected devices.
  * **Dynamic SNMP Polling:** A robust background service ([`SnmpPollingService.cs`]) polls devices at configurable intervals. It includes an intelligent **OID discovery mechanism** to adapt to different hardware configurations for RAM and multiple disk partitions (C, D, E).
  * **Intelligent Health Analysis:** Health status (`Healthy`, `Warning`, `Unreachable`) is determined in real-time after each poll by comparing live metrics against warning thresholds.
  * **User-Configurable Thresholds:** Features a hybrid system with global default thresholds that can be overridden on a per-device basis through the UI.
  * **Dual-Channel Alerting System:**
      * **In-App Notifications:** Uses **SignalR** to push instant, non-blocking toast notifications to the web UI for any device in a warning state.
      * **Smart Email Alerts:** Uses **MailKit** to send a detailed email alert only after a device has remained in a warning state for a configurable duration, preventing alert fatigue.
  * **Advanced PDF Reporting:** Generates two types of PDF reports using the QuestPDF library: a general summary report of all active devices and a detailed historical performance report for a single device over a user-selected time range.
  * **Responsive Design:** The UI is built with Bootstrap 5 and custom CSS to be fully responsive, including a "stacking card" table layout for a seamless experience on mobile devices.
  * **Secure User Authentication:** A complete user registration and login system protects access to the dashboard, featuring server-side password hashing and a secure "generate-and-email" flow for new user onboarding.

## 🛠️ Tech Stack

| Technology              | Description                                        |
| ----------------------- | -------------------------------------------------- |
| **ASP.NET Core Web API**| Backend framework for RESTful API development.     |
| **Entity Framework Core** | ORM for database management (Code-First).          |
| **MS SQL Server / LocalDB**| Relational database for storing all application data.|
| **SignalR** | For real-time, bi-directional web communication.   |
| **Lextm.SharpSnmpLib** | Library for all SNMP-based device communication.   |
| **QuestPDF** | Library for generating PDF reports from C\# code.   |
| **MailKit** | Library for sending emails via SMTP.             |
| **JavaScript (ES6+)** | For all client-side interactivity and API calls.   |
| **Bootstrap 5** | Frontend UI styling and responsive components.     |

## ⚙️ Setup and Configuration

Follow these steps to set up and run the project locally.

### 1\. Prerequisites

  * .NET SDK (The version targeted by the project, e.g., .NET 8 or 9)
  * SQL Server Express LocalDB (Typically installed with Visual Studio)

### 2\. Clone the Repository

```bash
git clone <your-repository-url>
cd <your-project-folder>
```

### 3\. Configure Application Settings

Open the **`appsettings.json`** file and configure the following three sections. For development, it is highly recommended to use the .NET Secret Manager for the `EmailSettings:Password`.

#### **Database Connection String**

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Device_Tracker;Trusted_Connection=True;MultipleActiveResultSets=true"
},
```

#### **Email Service Settings**

*(For Gmail, you must generate a special "App Password" instead of using your main password)*

```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "Port": 587,
  "SenderName": "Device Monitor Alert",
  "SenderEmail": "your-sending-email@gmail.com",
  "Username": "your-sending-email@gmail.com",
  "Password": "YOUR_APP_PASSWORD_HERE",
  "AlertRecipientEmail": "admin-to-receive-alerts@example.com"
},
```

#### **Default Monitoring Settings**

```json
"MonitoringSettings": {
  "DefaultCpuWarningThreshold": 80,
  "DefaultRamWarningThreshold": 85,
  "DefaultDiskWarningThreshold": 80,
  "EmailAlertWarningMinutes": 5
}
```

### 4\. Set Up the Database

This project uses a Code-First approach. The database will be created for you from the C\# models.

1.  Open the project in Visual Studio.
2.  Open the **Package Manager Console** (`View > Other Windows > Package Manager Console`).
3.  Run the following command to create and seed the database:
    ```powershell
    Update-Database
    ```

### 5\. Run the Application

You can now run the application by pressing **F5** in Visual Studio or by using the .NET CLI command:

```bash
dotnet run
```

-----

## 🚀 Getting Started

The application seeds a default administrator account upon first run if no other admin exists.

  * **Username:** `admin`
  * **Password:** `123456789` (or as configured in your database seeding logic)

It is highly recommended to log in and change the default password immediately. You can then begin adding devices to monitor.
