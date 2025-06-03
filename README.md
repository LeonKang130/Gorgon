# Gorgon: Rewriting Shader Functions Using Equality Saturation

## How to Build & Run

This project is written in `F#` and tested on Windows 11. After cloning the repository, run `dotnet build` to download dependencies and build the project.

>    Make sure to install .NET SDK 9.0 for your machine following the instructions here: https://learn.microsoft.com/en-us/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website

After successfully building the project, try run the following commands:

-   `dotnet run -- [--shader] ./Examples/slang/aces.slang ./aces.dsl`: translate `aces.slang` to `egglog` DSL script in `aces.dsl`

-   `dotnet run --dsl ./Examples/egg/aces.txt ./aces.slang`: translate the `egglog` DSL expression in `aces.txt` to `slang` in `aces.slang`

-   `dotnet test`: show the results of our benchmarks, which includes the original shader function, the preprocessed version after common subexpression elimination and constant folding, and the rewritten version with a help of equality saturation using `egglog`

## Pipeline & Benchmarks

Our project takes a function written in `slang` as input and outputs the optimized version based on a customizable cost model. Our pipeline is as follows:

1.   Parsing shader language into intermediate representation (IR)

2.   Preprocessing IR by building an DAG and retrieving the nodes reachable from the root

3.   Translating the IR into `egglog` DSL script accompanied with a set of rewrite rules

4.   Using `egglog` to perform equality saturation and extracting the rewritten expression

5.   Parsing the optimized expression in `egglog` DSL into IR

6.   Postprocessing IR by once again building an DAG

7.   Emit optimized shader function in `slang` from the DAG

More details and examples of our pipeline can be found in `Test.fs`.

>   Currently our pipeline is not fully automatic (since there is no binding for `egg` or `egglog` in .NET at the moment), so you need to use the output DSL script in *step 3* as the input for an independently deployed `egglog` (such as [the web demo](https://egraphs-good.github.io/egglog/)) and then input the extracted expression from `egglog` back to our application.

Our benchmarks can be found in directory `Examples/`:

-   `Examples/slang/` contains the original unoptimized slang code
-   `Examples/egg/` contains the DSL for each corresponding `.slang` file after our custom optimizations and `egglog`'s equality saturation (gotten from the browser app)

## Codebase Structure

### Intermediate Representation (IR)

Internally the shader functions are represented using an IR format defined in `IR.fs`. We also included a data structure denoting the weights (costs) assigned to each type of operation in the IR as well as a helper function to evaluate the total cost of a single function.

### Parsing & Printing (I/O)

Our pipeline relies on two parsers and two printers to enable the conversion from `slang` and `egglog` DSL to our IR and the other way around.

>   See [this page](https://egglog-python.readthedocs.io/latest/reference/egglog-translation.html) for more details on the syntax of `egglog` DSL. More example scripts can be found at [the web demo](https://egraphs-good.github.io/egglog/) as well.

The implementation of the parsers (based on `FParsec`) can be found in `Parser.fs` and the printers can be found in `Printer.fs`.

### Common Subexpression Elimination & Constant Folding

Before translating our IR to the DSL and the final optimized shader function, we perform pre and postprocessing by constructing an DAG based on the IR and recursively traversing the nodes reachable from the root (returned) expression.

During this process, the parts of the DAG unreachable from the root (also known as "dead code") are culled off and subgraphs containing only constant literals are evaluated and merged into a single constant literal. Moreover, we rename each subexpression and make sure exactly one local bindings exists for of them, so that shared subexpressions in the DAG are computed only once.

In this way, we are able to shrink the size of the e-graph handled by `egglog` and also eliminate the common subexpressions introduced by `egglog`. Details can be found in `CSE.fs`. 





