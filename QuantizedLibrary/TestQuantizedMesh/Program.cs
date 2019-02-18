using QuantizedMesh;
using System;

namespace TestQuantizedMesh
{
    class Program
    {
        static void Main(string[] args)
        {
            //var decoder = new QuantizedMeshFormatDecoder(new System.IO.FileInfo(@"C:\\development\\quantized-mesh-decoder-master\\src\\assets\\tile-with-extensions.terrain", false));
            var decoder = new QuantizedMeshFormatDecoder(new System.IO.FileInfo(@"C:\dem\DenHaag\output\14\8385\10979.terrain"),false);
        }
    }
}
