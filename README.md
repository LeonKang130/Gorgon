# Installation and Execution
Make sure to install .NET SDK 9.0 for your machine following the instructions here: https://learn.microsoft.com/en-us/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website


Run `dotnet build` to build the project.

Run `dotnet run ./Examples/slang/aces.slang ./aces.dsl` to translate `aces.slang` to egglog dsl in `aces.dsl`.

Run `dotnet run --dsl ./Examples/egg/aces.txt ./aces.slang` to translate the egglog dsl in `aces.txt` to slang in `aces.slang`.

Run `dotnet test` to see the results of our benchmarks.

We show an original shader function, then we show the re-written expression after
eliminating common sub-expressions and constant folding, and then finally we show a function re-written with the help of equality saturation using egg.


# Benchmarks and pipeline of our codebase

The pipeline of our codebase is that it takes a .slang file as an input,
translates it into an intermediate representation, performs common sub-expression removal and constant folding, then outputs the program in the egg dsl along with re-writing rules, and a cost model.

Then from there you take the outputted DSL and input it here: https://egraphs-good.github.io/egglog/
from which you will get a further optimized re-write using egg's equality saturation. Because F# has no egglog bindings, we have to use the web version for now.

Then from there we can parse that DSL and output a new slang file.

Our benchmarks are contained in the `Examples/` folder. The `slang/` folder contains the original unoptimized slang code. The `egg/` folder contains the DSL for each corresponding .slang file after our custom optimizations and egglog's equality saturation (gotten from the browser app).


# Structure of codebase

### `Parser.fs` and `IR.fs` files

The `Parser.fs` file defines the parser rules for a shader language. We parse it into an intermediate representation in `IR.fs` where we also define a function that calculates the cost of the function based on a cost model we have defined. This allows us to plug in different cost models, key to the modularity requirement in the original problem.

Additionally the `Parser.fs` file contains a parser for the egglog DSL language so that we can
translate optimized expressions output from the egglog DSL back into the shader language.



### `Printer.fs` file

This file translates the IR into the egglog DSL and includes relevant re-writing rules as well as the cost model for egglog to operate on. This is put in the file `dsl.txt`. This egglog DSL can then be input the egglog web application here: https://egraphs-good.github.io/egglog/. From here, an optimized re-written expression is produced using equality saturation. Unfortunately F# does not have egglog bindings, so until those are written, we use the website as a proof of concept.

This outputted DSL is translated back into the shader language using functionality from the `Parser.fs` file.



### `CSE.fs` file

This file does constant folding on the IR as well as eliminating duplicate terms. Both these procedures will reduce the operations in the source code, lowering the total cost of the function.





