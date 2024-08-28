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

### Default Flag Values

- `--endpoint`: "http://localhost:5001".
- `--taskId`: "none".
- `--dataFolder`: The current directory + "ak_dumper_" + taskId. Example : " /mnt/c/Users/ereali/source/repos/ak_dumper_2bc1bd28-9082-45fd-9390-ac713682e038".

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
    "dataFolder": "/mnt/c/Users/ereali/source/repos/ak_dumper_2bc1bd28-9082-45fd-9390-ac713682e038/Results",
    "configuration": {}
}
```

### **Put the values in files**

- The payload data needs to be in a file named after the `<PAYLOAD_ID>`. This `<PAYLOAD_ID>` will be put in the `PayloadId` section of your JSON.
- The data dependencies need to be in files named after each `<DATADEPENDENCIE_ID>`. These `<DATADEPENDENCIE_ID>` will be put in the `DataDependencies` section of your JSON.

Create these files in the same folder and set its path in the **dataFolder** field of the JSON file.

Tree Example :

```
ak_dumper_2bc1bd28-9082-45fd-9390-ac713682e038
├── Results
    ├── PAYLOAD_ID
    └── DATADEPENDENCIE_ID
```


## **Start Your Worker**

Create a directory with write access fot sockets.

```bash
mkdir /tmp/sockets
chmod 777 /tmp/sockets
```

**Linux** with the sockets path `ComputePlane__WorkerChannel__Address=/tmp/sockets/worker.sock` and `ComputePlane__AgentChannel__Address=/tmp/sockets/agent.sock`.

  Here is a command line example to run a C# worker with the correct sockets:

```sh
ComputePlane__WorkerChannel__Address=/tmp/sockets/worker.sock \
ComputePlane__AgentChannel__Address=/tmp/sockets/agent.sock \
dotnet run --project <PATH_TO_PROJECT.CSPROJ>
```

Replace `<PATH_TO_PROJECT.CSPROJ>` with the path to your .csproj file.

**Docker** 
  Htcmock through docker:

```sh
docker run --rm -d --name htcmock -u $(id -u) \
-e ComputePlane__WorkerChannel__Address=/cache/worker.sock \
-e ComputePlane__AgentChannel__Address=/cache/agent.sock \
-v /tmp/sockets:/cache -v <PATH_TO_BINARIES>:<PATH_TO_BINARIES> \
dockerhubaneo/armonik_core_htcmock_test_worker:<ARMONIK_VERSION_CORE>
```

Replace `<PATH_TO_BINARIES>` with the path to the extracted binary files.

Replace `<ARMONIK_VERSION>` with the [current core version](https://github.com/aneoconsulting/ArmoniK/blob/main/versions.tfvars.json).


# Usage

## Rerun a Single Task

To rerun a single task, use the `dotnet run` command and provide the path of the JSON file containing task data as an argument:

```sh
dotnet run --path <DATA.JSON> --project src/TaskReRunner/
```

or with the .Net tool :

```sh
TaskReRunner --path <DATA.JSON>
```

Replace `<DATA.JSON>` with the path to your JSON file.

Replace `<PATH_TO_FOLDER>` with the absolute path to your folder containing `PayloadId` and `DataDependencies` files.

## Flags

- `--path`: Path to the JSON file containing the data needed to rerun the task.

### Default Flag Values

- `--path`: "task.json".

----

You can recover your results in `Data_Folder_Path/Expected_Output_Id` or it will be printed on your console as:
`Notified result0 Data: <DATA_IN_BYTE>`.

