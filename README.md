# Armonik.Task.ReRunner

This project is part of the [ArmoniK](https://github.com/aneoconsulting/ArmoniK) project.
Armonik.Task.ReRunner is designed to simplify debugging by allowing users to rerun previously submitted and processed tasks. By providing services to select and re-execute individual tasks, it helps users identify and fix issues, streamlining the overall debugging process.

You can use your favorite IDE debugger on you worker with the ArmoniK.TaskReRunner.

# Key Features

- **Rerun Individual Tasks:** Select and rerun individual ArmoniK tasks by providing their task IDs.

- **Logging and Monitoring:** Detailed logs and monitoring for each rerun operation to ensure debugging and transparency.

# Installation


- Install the .Net tool
   _From [nuget.org](https://www.nuget.org/) in command line :_

    ```sh
    dotnet tool install -g ArmoniK.TaskReRunner
    ```

Or :

- Clone the repository and move into it.

    ```sh
    git clone git@github.com:aneoconsulting/ArmoniK.TaskReRunner.git
    cd ArmoniK.TaskReRunner
    ```

# Prerequisites

To run the program, you need:
- To have a task that you want to rerun with its **TaskId**.
- To have a worker which **failed**.
- To extract the data from the task you want to rerun in the correct **JSON** format.

*N.B.: You don't need ArmoniK to rerun a task, but **you can't rerun a task without having already run it through ArmoniK.***

## **Start Your Worker**

**Linux** with the sockets path `ComputePlane__WorkerChannel__Address=/tmp/worker.sock` and `ComputePlane__AgentChannel__Address=/tmp/agent.sock`.

- Here is a command line example to run a C# worker with the correct sockets:

    ```sh
    ComputePlane__WorkerChannel__Address=/tmp/worker.sock ComputePlane__AgentChannel__Address=/tmp/agent.sock dotnet run --project <PATH_TO_PROJECT.CSPROJ>
    ```

Replace `<PATH_TO_PROJECT.CSPROJ>` with the path to your .csproj file.

## Obtain Data in Json

### Use the TaskDumper to extract Data from ArmoniK.

Run the TaskDumper program with your **ArmoniK running**.

- With the .Net tool :

Install the .Net tool
   _From [nuget.org](https://www.nuget.org/) in command line :_

```sh
dotnet tool install -g ArmoniK.TaskDumper
```

```sh
TaskDumper --endpoint <YOUR_ENDPOINT> --taskId <TASK_ID>
```

- With the cloned repository :

```sh
dotnet run --project src/TaskDumper --endpoint <YOUR_ENDPOINT> --taskId <TASK_ID>
```

Replace `<YOUR_ENDPOINT>` with your control_plane_url.

Replace `<TASK_ID>` with TaskId of the task to retrieve.

### Flags

- `--endpoint`: Endpoint for the connection to ArmoniK control plane.
- `--taskId`: TaskId of the task to retrieve.
- `--dataFolder`: Absolute path to the folder that will contain the binary data required to rerun the task.
- `--name`: The name of the JSON file to be created.

### Default Flag Values

- `--endpoint`: "http://localhost:5001".
- `--taskId`: "none".
- `--dataFolder`: The system temporary directory, for example, `/tmp` on Unix-based systems.
- `--name`: "".

## Debug Without Extracting Directly from ArmoniK

### **Create Your Own JSON**

You can create a JSON yourself.

Here is a minimalist JSON with only the necessary variables for the HelloWorld Sample Example:

```json
{
    "sessionId": "",
    "payloadId": "2bc1bd28-9082-45fd-9390-ac713682e038",
    "taskId": "",
    "taskOptions": {},
    "dataDependencies": [],
    "expectedOutputKeys": [
        "8cdc1510-2240-488e-bd5f-4df0969ef5e9"
    ],
    "configuration": {}
}
```

### **Put the values in files**

- The payload data needs to be in a file named after the `<PAYLOAD_ID>`. This `<PAYLOAD_ID>` will be put in the `PayloadId` section of your JSON.
- The data dependencies need to be in files named after each `<DATADEPENDENCIE_ID>`. These `<DATADEPENDENCIE_ID>` will be put in the `DataDependencies` section of your JSON.

Create those files in the same folder.

Tree Example :

```
├── tmp
    ├── PAYLOAD_ID
    └── DATADEPENDENCIE_ID
```

# Usage

## Rerun a Single Task

To rerun a single task, use the `dotnet run` command and provide the path of the JSON file containing task data as an argument:

```sh
dotnet run --path <DATA.JSON> --dataFolder <PATH_TO_FOLDER> --project src/TaskReRunner/
```

or with the .Net tool :

```sh
TaskReRunner --path <DATA.JSON> --dataFolder <PATH_TO_FOLDER>
```

Replace `<DATA.JSON>` with the path to your JSON file.

Replace `<PATH_TO_FOLDER>` with the absolute path to your folder containing `PayloadId` and `DataDependencies` files.

## Flags

- `--path`: Path to the JSON file containing the data needed to rerun the task.
- `--dataFolder`: Absolute path to the folder that will contain the binary data required to rerun the task.

### Default Flag Values

- `--path`: "Task_Id.json".
- `--dataFolder`: Your temporary user directory, for example, on Linux, `/tmp/`.

----

You can recover your results in `Data_Folder_Path/Expected_Output_Id` or it will be printed on your console as:
`Notified result0 Data: <DATA_IN_BYTE>`.

