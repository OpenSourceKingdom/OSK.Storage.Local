﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local
{
    public static class MemoryStreamExtensions
    {
        public static async Task<byte[]> ToArrayAsync(this MemoryStream memoryStream, int startIndex = 0, 
            int bytesReadPerIteration = 1024, CancellationToken cancellationToken = default)
        {
            memoryStream.Position = startIndex;

            var byteArray = new byte[ memoryStream.Length - startIndex ];
            var currentIndex = 0;
            do
            {
                var byteReadCount = (int)Math.Min(bytesReadPerIteration, memoryStream.Length - startIndex - currentIndex);
                await memoryStream.ReadAsync(byteArray, currentIndex, byteReadCount, cancellationToken);
                currentIndex += byteReadCount;
            } while (currentIndex + startIndex < memoryStream.Length);

            return byteArray;
        }
    }
}
