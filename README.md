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

    This project requires a Worker you need to launch it with the sockets:

    ```sh
    ComputePlane__WorkerChannel__Address=/tmp/worker.sock ComputePlane__AgentChannel__Address=/tmp/agent.sock dotnet run --project <PATH_PROJECT.CSPROJ>
    ```

## Usage

### Rerun a Single Task

To rerun a single task, use the `TaskReRunner` dotnet tool and provide the path of the json file containing taskData as an argument:

```sh
TaskReRunner --path <DATA.JSON>
```

## Flags

- `--path` : Path to the json file containing the data needed to rerun the Task.
- `--dataFolder` : Absolute path to the folder created to contain the binary data required to rerun the Task.

### Default Flags Value

- `--path` : "Data.json".
- `--dataFolder` : A tmp generate subdirectory, exemple `/tmp/xiazOE`.
