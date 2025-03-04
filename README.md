# FileMoverService

**FileMoverService** is a versatile Windows service designed to automatically monitor and manage files in specific directories. It can be used to handle all types of file formats based on predefined rules, making it adaptable for a variety of use cases, such as organizing downloads, backing up files, or moving processed data to another location.

---

## **Features**

- Monitors specific directories for file changes.
- Automatically moves or processes files based on configuration.
- Supports any file format through configurable settings.
- Runs as a lightweight Windows service using [Topshelf](https://github.com/Topshelf/Topshelf).
- Customizable and scalable for various automation needs.
- Starts automatically on system boot.

---

## **Installation**

1. Clone the repository from GitHub:
   ```bash
   git clone https://github.com/benedektothten/file-mover-service.git
   ```
2. Build the project using Visual Studio or your preferred .NET IDE.
3. Open a command prompt with administrative privileges.
4. Navigate to the folder containing the built `.exe` file.
5. Install the service:
   ```bash
   FileMoverService.exe install
   ```
6. Start the service:
   ```bash
   FileMoverService.exe start
   ```

---

### **Uninstallation**

1. Stop the service:
   ```bash
   FileMoverService.exe stop
   ```
2. Uninstall the service:
   ```bash
   FileMoverService.exe uninstall
   ```

---

## **Configuration**

The service uses an `appsettings.json` configuration file to determine its behavior. You can specify the directories to monitor, supported file formats, and target paths to move files.

### **Example AppSettings**
```json
{
  "AppSettings": {
    "SourceDirectory": "C:\\Path\\To\\Source\\Directory",
    "DestinationDirectory": "D:\\Path\\To\\Destination\\Directory",
    "FileExtensions": [".jpg", ".png", ".txt", ".pdf", ".docx"]
  }
}
```

### **Configuration Steps**

1. Locate the `appsettings.json` file in the root directory of the service executable.
2. Update the following fields based on your needs:
   - **`SourceDirectory`**: The directory that the service will monitor.
   - **`DestinationDirectory`**: The directory where matching files will be moved.
   - **`FileExtensions`**: A list of file types that the service should monitor and process (e.g., `.jpg`, `.txt`, `.mp4`).
3. Save the changes in the file.
4. Restart the service to apply the updated configuration:
   ```bash
   FileMoverService.exe stop
   FileMoverService.exe start
   ```

---

## **Running the Service**

You can manage the service using the command line or the Windows Services application.

### **Command Line Usage**
- **Start the Service**:
  ```bash
  FileMoverService.exe start
  ```
- **Stop the Service**:
  ```bash
  FileMoverService.exe stop
  ```
- **Restart the Service**:
  ```bash
  FileMoverService.exe restart
  ```
- **Check Service Status**:
  ```bash
  FileMoverService.exe status
  ```

### Using the Windows Services Application
1. Open the `Run` dialog (Press `Win + R`).
2. Type `services.msc` and press Enter.
3. Locate the service named **"File Mover Service"**.
4. Right-click on the service to start, stop, or restart it.

---

## **Requirements**

- Windows OS.
- .NET 9.0 runtime installed.
- Administrative privileges for installation and service management.

---

## **Logs and Troubleshooting**

The service generates log files to aid in identifying any issues or tracking its behavior. Logs are stored based on your logging configuration.

### **Default Log Location**
Logs will be placed in the same directory as the service executable unless otherwise configured.

### **Common Issues**
1. **Permission Errors**:
   Ensure that the service has the necessary permissions to access the `SourceDirectory` and `DestinationDirectory`.
2. **Configuration Issues**:
   Verify that the `appsettings.json` file is properly formatted and all required fields are filled.
3. **Service Fails to Start**:
   Check the logs for error details.

---

## **Use Cases for FileMoverService**

- **Download Management**: Automatically sort and organize downloaded files into specific directories.
- **Backup Solutions**: Move files to a backup directory for safekeeping.
- **Media Organization**: Organize various media files (images, videos, documents) by moving them to their respective folders.
- **Custom Use**: Adapt the service for any automation task that involves moving or processing files.

---

## **Contributing**

We welcome contributions! Here's how you can get involved:
1. Fork the repository.
2. Create a new branch with your change or feature.
3. Push your changes to your branch.
4. Submit a pull request for review.

---

## **License**

This project is licensed under the MIT License. Refer to the `LICENSE` file for further details.