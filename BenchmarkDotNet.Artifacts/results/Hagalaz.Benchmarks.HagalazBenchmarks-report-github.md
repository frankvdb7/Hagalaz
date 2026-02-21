```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.326.7603), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.3 (10.0.326.7603), X64 RyuJIT AVX2


```
| Method                   | N    | Mean         | Error      | StdDev     | Allocated |
|------------------------- |----- |-------------:|-----------:|-----------:|----------:|
| **ListContains**             | **100**  |     **9.475 ns** |  **0.0137 ns** |  **0.0107 ns** |         **-** |
| ListHashSetContains      | 100  |     4.434 ns |  0.0029 ns |  0.0025 ns |         - |
| Viewport_Old_List        | 100  | 2,409.476 ns | 11.3857 ns | 10.6502 ns |         - |
| Viewport_New_ListHashSet | 100  |   941.638 ns |  0.9242 ns |  0.7717 ns |         - |
| **ListContains**             | **1000** |    **57.126 ns** |  **1.1240 ns** |  **1.2494 ns** |         **-** |
| ListHashSetContains      | 1000 |     3.676 ns |  0.0029 ns |  0.0026 ns |         - |
| Viewport_Old_List        | 1000 | 9,857.797 ns | 61.9023 ns | 54.8748 ns |         - |
| Viewport_New_ListHashSet | 1000 | 1,130.422 ns |  1.0466 ns |  0.9278 ns |         - |
