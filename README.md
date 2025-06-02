# Installation and Execution
Make sure to install .NET SDK 9.0 for your machine following the instructions here: https://learn.microsoft.com/en-us/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website


Run `dotnet build` to build the project.

Run `dotnet run Gorgon` to run the project.

# Structure of codebase

### `Parser.fs` and `IR.fs` files

The `Parser.fs` file defines the parser rules for our crypto/shader language. We parse it into an intermediate representation in `IR.fs` where we also define a function that calculates the cost of the function based on a cost model we have defined. This allows us to plug in different cost models, key to the modularity requirement in the original problem.

### `CSE.fs` file

This file does constant folding on the IR as well as eliminating duplicate terms. Both these procedures will reduce the operations in the source code, lowering the total cost of the function.

### `Printer.fs` file

This file translates the IR into the egglog DSL and includes relevant re-writing rules as well as the cost model for egglog to operate on. This is put in the file `dsl.txt`. This egglog DSL can then be input the egglog web application here: https://egraphs-good.github.io/egglog/. From here, an optimized re-written expression is produced using equality saturation. Unfortunately F# does not have egglog bindings, so until those are written, we use the website as a proof of concept.




