# Smart-X Gateway


A premium, modern industrial IoT platform for monitoring and managing sensor networks in real-time. Smart-X Gateway connects thousands of IoT sensors across your infrastructure, providing real-time telemetry, anomaly detection, and network health monitoring.

Link to GitHub: https://github.com/EMWCCN/PROG7312_POE_Gr2_St10287116.git4
Link to youtube: https://www.youtube.com/watch?v=62TH5MTy8qI

## Project Overview

Smart-X Gateway is a full-stack web application built with:
- **Backend API**: ASP.NET Core 10.0 Web API (`SmartX.API`)
- **Frontend UI**: Blazor Server with interactive components (`SmartX.Client`)
- **Database**: In-memory storage for development
- **Architecture**: Modern SPA with responsive design

### Key Features

- Real-time sensor network monitoring
- Telemetry data ingestion and processing
- Color-coded severity indicators (normal, warning, critical)
- Sensor registration and management
- File upload capability for logs and photos
- Live telemetry streaming with 5-second auto-refresh
- Premium, modern UI with glassmorphism design
- Fully responsive on desktop, tablet, and mobile

---

## Prerequisites

Before you begin, ensure you have the following installed on your system:

### Required

- **.NET SDK 10.0 or higher**
  - Download from: https://dotnet.microsoft.com/download
  - Verify installation: `dotnet --version`

- **Visual Studio 2022 Community Edition (or later)** - Recommended
  - Download from: https://visualstudio.microsoft.com/
  - OR use Visual Studio Code with C# extension

### Optional (but recommended)

- **Git** - For version control
  - Download from: https://git-scm.com/
- **Postman** - For API testing
  - Download from: https://www.postman.com/

---

## Project Structure

```
SmartX/
├── SmartX.API/                          # Backend API Project
│   ├── Program.cs                       # Application startup configuration
│   ├── Properties/
│   │   └── launchSettings.json         # Launch configuration
│   ├── Models/
│   │   ├── SensorRecord.cs
│   │   ├── TelemetryPacket.cs
│   │   └── DeploymentValidator.cs
│   ├── SensorRegistry.cs                # Core business logic
│   ├── appsettings.json                 # Application settings
│   └── appsettings.Development.json     # Development settings
│
├── SmartX.Client/                       # Frontend Blazor Project
│   ├── Program.cs                       # Blazor app configuration
│   ├── Components/
│   │   ├── App.razor                    # Root component
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor
│   │   │   ├── NavMenu.razor
│   │   │   └── ReconnectModal.razor
│   │   └── Pages/
│   │       ├── Home.razor               # Dashboard
│   │       ├── Sensors.razor            # Sensor management
│   │       ├── Counter.razor
│   │       ├── Weather.razor
│   │       └── Error.razor
│   ├── Services/
│   │   └── SmartXApiClient.cs           # API communication layer
│   ├── Models/
│   │   └── SmartXModels.cs              # Shared data models
│   ├── wwwroot/
│   │   ├── app.css                      # Premium design system
│   │   ├── index.html
│   │   └── favicon.png
│   ├── Properties/
│   │   └── launchSettings.json
│   └── appsettings.json
│
└── README.md                             # This file
```

---

## Quick Start Guide

Follow these steps to get Smart-X Gateway running on your local machine.

### Step 1: Clone or Extract the Project

If using Git:
```
git clone <repository-url>
cd SmartX
```

If using extracted files:
```
cd /path/to/SmartX
```

### Step 2: Restore Dependencies

Navigate to the project root directory and restore all NuGet packages for both projects.

**For the API:**
```
cd SmartX.API
dotnet restore
cd ..
```

**For the Client:**
```
cd SmartX.Client
dotnet restore
cd ..
```

Or restore both at once from the root directory:
```
dotnet restore
```

This command will:
- Download all required NuGet packages
- Restore project dependencies
- Prepare the project for compilation

---

### Step 3: Build the Project

Build both projects to ensure everything compiles correctly.

**Build the API:**
```
cd SmartX.API
dotnet build
cd ..
```

**Build the Client:**
```
cd SmartX.Client
dotnet build
cd ..
```

Or build both from the root:
```
dotnet build
```

**Expected Output:**
```
Build succeeded.
0 Warning(s)
0 Error(s)
```

If you see any errors, check:
- .NET SDK version matches (10.0+)
- All dependencies were restored correctly
- No file path issues

---

### Step 4: Start the Backend API

The backend API must be running before starting the client. The API runs on `https://localhost:7008`.

**Open Terminal 1 and run:**

```
cd SmartX.API
dotnet run
```

**Expected Output:**
```
info: Microsoft.AspNetCore.Hosting.Hosting[14]
	  Now listening on: https://localhost:7008
info: Microsoft.AspNetCore.Hosting.Hosting[14]
	  Now listening on: http://localhost:5043
```

**Verify the API is running:**

Open your browser and navigate to:
- https://localhost:7008/swagger/ui.html (Swagger API documentation)

You should see the API endpoints listed. The API is now ready to handle requests.

**Keep this terminal open while testing the application.**

---

### Step 5: Start the Client Application

In a separate terminal, run the Blazor client application.

**Open Terminal 2 and run:**

```
cd SmartX.Client
dotnet run
```

**Expected Output:**
```
info: Microsoft.AspNetCore.Hosting.Hosting[14]
	  Now listening on: https://localhost:7120
info: Microsoft.AspNetCore.Hosting.Hosting[14]
	  Now listening on: http://localhost:5202
```

The client will be available at:
- **HTTPS:** https://localhost:7120
- **HTTP:** http://localhost:5202

---

### Step 6: Access the Application

Open your web browser and navigate to:

```
https://localhost:7120
```

You should see the Smart-X Gateway dashboard with:
- Premium dark UI with cyan accents
- Hero section with system metrics
- Three capability cards (Sensor Network Control, Remote Control, Network Topology)
- Responsive navigation bar

---

## Using the Application

### Dashboard (Home Page)

The dashboard displays:
- **Hero Section**: System overview with connected sensors, network health, and gateway status
- **Feature Cards**: Available and upcoming features
- **Navigation**: Links to Sensors page and other sections

### Sensor Network Page

Access via: `https://localhost:7120/sensors`

**Features:**

1. **Register New Sensor**
   - Enter MAC Address (e.g., `AA:BB:CC:00:11:22`)
   - Enter Deployment Location (e.g., `SubZoneB/Zone1/FacilityA`)
   - Select Sensor Category (Environmental, PowerConsumption, Actuator)
   - Click "Register sensor"

2. **Connected Sensors**
   - View all registered sensors in a table
   - See device IDs, locations, and categories
   - Upload logs or photos for each sensor

3. **Live Telemetry Stream**
   - Real-time sensor readings with color-coded severity
   - Green: Normal readings
   - Amber: Warning range
   - Red: Critical readings
   - Click "Simulate reading" to test with demo data

---

## Configuration

### API Configuration

**File:** `SmartX.API/appsettings.json`

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft.AspNetCore": "Warning"
	}
  },
  "AllowedHosts": "*"
}
```

### Client Configuration

**File:** `SmartX.Client/appsettings.json`

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft.AspNetCore": "Warning"
	}
  },
  "AllowedHosts": "*"
}
```

**API Base URL Configuration:**

Edit `SmartX.Client/Program.cs`:

```csharp
builder.Services.AddHttpClient<SmartXApiClient>(client =>
{
	client.BaseAddress = new Uri(
		builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7008");
});
```

Change the port if your API runs on a different port.

---

## API Endpoints

The Smart-X API provides the following endpoints:

### Sensor Endpoints

- **GET** `/api/sensors` - Get all registered sensors
- **POST** `/api/sensors` - Register a new sensor
- **GET** `/api/sensors/{mac}` - Get specific sensor by MAC address
- **POST** `/api/sensors/{mac}/upload` - Upload file to sensor

### Telemetry Endpoints

- **POST** `/api/telemetry` - Submit telemetry reading
- **GET** `/api/telemetry/{sensorId}` - Get telemetry history for sensor

---

## Troubleshooting

### Issue: Port Already in Use

**Error:** `Address already in use: https://localhost:7008`

**Solution:**
```
# Find process using the port (Windows PowerShell)
Get-NetTCPConnection -LocalPort 7008

# Kill the process
Stop-Process -Id <PID> -Force

# Or use a different port by modifying launchSettings.json
```

### Issue: API Connection Failed

**Error:** `HttpRequestException: No connection could be made`

**Solution:**
1. Ensure the API is running: Check Terminal 1 shows "Now listening on"
2. Verify the port matches in Client configuration (should be 7008)
3. Clear browser cache (Ctrl+Shift+Delete)
4. Check firewall isn't blocking the connection

### Issue: Build Errors

**Error:** `Build failed`

**Solution:**
```
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build

# If still failing, check SDK version
dotnet --version  # Should be 10.0 or higher
```

### Issue: CSS Not Loading

**Error:** Styles appear broken or missing

**Solution:**
1. Clear browser cache (Ctrl+Shift+Delete)
2. Hard refresh the page (Ctrl+F5 or Cmd+Shift+R)
3. Check `app.css` exists in `SmartX.Client/wwwroot/`

### Issue: Cannot Connect to Database

**Note:** SmartX Gateway uses in-memory storage. No database setup is required. All data is stored in memory and will be cleared when the application restarts.

---

## Development Notes

### Architecture

**Three-Tier Architecture:**

1. **Presentation Layer (Client)**
   - Blazor Server components
   - Responsive UI with CSS
   - Real-time updates using SignalR

2. **Business Logic Layer (Services)**
   - SmartXApiClient for API communication
   - Data validation and transformation
   - Event handling

3. **Data Access Layer (API)**
   - REST API endpoints
   - SensorRegistry for business logic
   - In-memory data storage

### Key Technologies

- **ASP.NET Core 10.0** - Server framework
- **Blazor Server** - Interactive web UI
- **C# 12** - Programming language
- **CSS3** - Styling with custom properties
- **SignalR** - Real-time communication

### Design System

- **Color Scheme**: Dark mode with cyan (#06b6d4) accents
- **Typography**: System fonts for performance
- **Spacing**: 8px-based spacing scale
- **Responsive**: Mobile-first approach with breakpoints at 768px and 1024px
- **Effects**: Glassmorphism, gradients, smooth animations

---

## Performance Considerations

- **API Response Time**: < 100ms for most operations
- **Client Build Size**: ~2.5 MB (JS/WASM)
- **CSS Size**: ~35 KB (uncompressed)
- **Database**: In-memory (no disk I/O)
- **Polling Interval**: 5-second auto-refresh for sensors

---

## Security Notes

This is a development build. For production deployment:

1. **HTTPS Only**: Enable SSL/TLS certificates
2. **Authentication**: Add user authentication and authorization
3. **CORS**: Restrict cross-origin requests appropriately
4. **Input Validation**: Validate all user inputs
5. **Rate Limiting**: Implement rate limiting for API endpoints
6. **Logging**: Set up comprehensive logging and monitoring

---

## Browser Support

- **Chrome/Edge**: 88+
- **Firefox**: 87+
- **Safari**: 14+
- **Mobile Browsers**: All modern versions

---

## Deployment

### Local Development

Already covered above - just run both projects locally.

### IIS Deployment (Windows)

1. Publish both projects:
```
dotnet publish -c Release
```

2. Configure IIS websites pointing to published folders
3. Install required ASP.NET Core hosting bundle
4. Configure SSL certificates

### Docker Deployment

Create a Dockerfile for containerization:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SmartX.API/SmartX.API.csproj", "SmartX.API/"]
RUN dotnet restore "SmartX.API/SmartX.API.csproj"
COPY . .
RUN dotnet build "SmartX.API/SmartX.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartX.API/SmartX.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 7008
ENTRYPOINT ["dotnet", "SmartX.API.dll"]
```

---

## Support & Documentation

### API Documentation

Access Swagger UI at: `https://localhost:7008/swagger/ui.html`

### Code Comments

All source files include inline comments explaining functionality.

### Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [.NET SDK Downloads](https://dotnet.microsoft.com/download)

---

## Frequently Asked Questions

**Q: Do I need SQL Server?**
A: No. Smart-X Gateway uses in-memory storage for development. No database is required.

**Q: Can I use HTTP instead of HTTPS?**
A: Yes, but HTTPS is recommended. Modify `launchSettings.json` to disable SSL.

**Q: How do I change the API port?**
A: Edit `SmartX.API/Properties/launchSettings.json` and update the port numbers.

**Q: How do I change the Client port?**
A: Edit `SmartX.Client/Properties/launchSettings.json` and update the port numbers.

**Q: Is there a database backup?**
A: No. In-memory storage means all data is lost on restart. For persistence, implement a real database.

**Q: How do I run tests?**
A: No unit tests are included in this version. Add test projects for comprehensive testing.

---

## License

This project is provided as-is for educational and development purposes.

---

## Version Information

- **Application Version**: 1.0
- **.NET Version**: 10.0+
- **Blazor Version**: Integrated with .NET 10.0
- **Last Updated**: 2024

---

## Quick Reference

### Essential Commands

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run API (Terminal 1)
cd SmartX.API && dotnet run

# Run Client (Terminal 2)
cd SmartX.Client && dotnet run

# Clean build
dotnet clean

# Publish for release
dotnet publish -c Release
```

### URLs

- **API Base**: https://localhost:7008
- **API Swagger**: https://localhost:7008/swagger/ui.html
- **Client UI**: https://localhost:7120
- **Dashboard**: https://localhost:7120/
- **Sensors**: https://localhost:7120/sensors

### Default Ports

- **API HTTPS**: 7008
- **API HTTP**: 5043
- **Client HTTPS**: 7120
- **Client HTTP**: 5202

---

## Getting Help

If you encounter issues:

1. Check the **Troubleshooting** section above
2. Verify all prerequisites are installed
3. Ensure both API and Client are running
4. Check browser console for JavaScript errors
5. Review debug output in terminal windows

---

**Ready to get started? Follow the Quick Start Guide above!**
