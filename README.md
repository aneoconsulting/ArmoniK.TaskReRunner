# Armonik.Task.ReRunner

This project is part of the  [ArmoniK](https://github.com/aneoconsulting/ArmoniK) project. Armonik.Task.ReRunner is designed to simplify the debugging process by providing functionality to rerun tasks that were previously submitted and processed.

## What is Armonik.Task.ReRunner ?

The Armonik Task ReRunner allows users to rerun tasks using ArmoniK.
It will provide services for selecting and rerunning individual tasks.
This feature ensures that users can easily re-execute tasks to identify and fix issues, thereby enhancing the overall debugging process.

## Key Features

- **Rerun Individual Tasks:** Select and rerun individual ArmoniK tasks by providing their task IDs.
- **Logging and Monitoring:** Detailed logs and monitoring for each rerun operation to ensure debug and transparency.

## Installation

1. **Install the dotnet tool**

    ```sh
    dotnet tool install -g ArmoniK.TaskReRunner
    ```

2. **Start your worker**

    This project requires a Worker launch with the sockets.
    
    Here an example in csharp:

    ```sh
    ComputePlane__WorkerChannel__Address=/tmp/worker.sock ComputePlane__AgentChannel__Address=/tmp/agent.sock dotnet run --project <PATH_PROJECT.CSPROJ>
    ```

    Replace <PATH_PROJECT.CSPROJ> with the path to your .csproj. 

## Prerequise

To run the Program you need:
- To have a Task that you want to rerun with its **TaskId**.
- To have a worker which **fail**.
- To extract the Data from the task you want to rerun in the correct **json** format.

*N.B : You don't need ArmoniK to rerun a Task but **you can't rerun a Task without have already run it through ArmoniK.***

## Usage

### Rerun a Single Task

To rerun a single task, use the `TaskReRunner` dotnet tool and provide the path of the json file containing taskData as an argument:

```sh
TaskReRunner --path <DATA.JSON>
```

## Flags

- `--path` : Path to the json file containing the data needed to rerun the Task.
- `--dataFolder` : Absolute path to the folder that will contain the binary data required to rerun the Task.

### Default Flags Value

- `--path` : "Data.json".
- `--dataFolder` : A `/tmp` generated subdirectory, exemple `/tmp/xiazOE`.



Certainly! Here is the corrected version of your message:

---

# Armonik.Task.ReRunner

This project is part of the [ArmoniK](https://github.com/aneoconsulting/ArmoniK) project. Armonik.Task.ReRunner is designed to simplify the debugging process by providing functionality to rerun tasks that were previously submitted and processed.

## What is Armonik.Task.ReRunner?

The Armonik Task ReRunner allows users to rerun tasks using ArmoniK. It provides services for selecting and rerunning individual tasks. This feature ensures that users can easily re-execute tasks to identify and fix issues, thereby enhancing the overall debugging process.

## Key Features

- **Rerun Individual Tasks:** Select and rerun individual ArmoniK tasks by providing their task IDs.
- **Logging and Monitoring:** Detailed logs and monitoring for each rerun operation to ensure debugging and transparency.

## Installation

1. **Install the .NET Tool**

    ```sh
    dotnet tool install -g ArmoniK.TaskReRunner
    ```

2. **Start Your Worker**

    This project requires a worker launched with the sockets `ComputePlane__WorkerChannel__Address=/tmp/worker.sock` and `ComputePlane__AgentChannel__Address=/tmp/agent.sock`.
    
    Here is a command line example to run a C# worker with the correct sockets :

    ```sh
    ComputePlane__WorkerChannel__Address=/tmp/worker.sock ComputePlane__AgentChannel__Address=/tmp/agent.sock dotnet run --project <PATH_TO_PROJECT.CSPROJ>
    ```

    Replace `<PATH_TO_PROJECT.CSPROJ>` with the path to your .csproj file.

## Prerequisites

To run the program, you need:
- To have a task that you want to rerun with its **TaskId**.
- To have a worker which **failed**.
- To extract the data from the task you want to rerun in the correct **JSON** format.

*N.B.: You don't need ArmoniK to rerun a task, but **you can't rerun a task without having already run it through ArmoniK.***

## Usage

### Rerun a Single Task

To rerun a single task, use the `TaskReRunner` .NET tool and provide the path of the JSON file containing task data as an argument:

```sh
TaskReRunner --path <DATA.JSON>
```

## Flags

- `--path`: Path to the JSON file containing the data needed to rerun the task.
- `--dataFolder`: Absolute path to the folder that will contain the binary data required to rerun the task.

### Default Flag Values

- `--path`: "Data.json".
- `--dataFolder`: A `/tmp` generated subdirectory, for example, `/tmp/xiazOE`.

---

Feel free to let me know if there's anything else you'd like to adjust!