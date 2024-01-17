﻿using Img2Ffu.Structures.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Img2Ffu.Structures.Data
{
    internal class SignedImage
    {
        public SecurityHeader SecurityHeader;
        public byte[] Catalog;
        public byte[] TableOfHashes;
        public byte[] Padding = [];
        public ImageFlash Image;

        public readonly List<byte[]> BlockHashes = [];
        public X509Certificate Certificate;

        private readonly Stream Stream;
        private readonly ulong DataBlocksPosition;

        public SignedImage(Stream stream)
        {
            Stream = stream;

            SecurityHeader = stream.ReadStructure<SecurityHeader>();

            if (SecurityHeader.Signature != "SignedImage ")
            {
                throw new InvalidDataException("Invalid Security Header Signature!");
            }

            Catalog = new byte[SecurityHeader.CatalogSize];
            _ = stream.Read(Catalog, 0, (int)SecurityHeader.CatalogSize);

            TableOfHashes = new byte[SecurityHeader.HashTableSize];
            _ = stream.Read(TableOfHashes, 0, (int)SecurityHeader.HashTableSize);

            long Position = stream.Position;
            uint sizeOfBlock = SecurityHeader.ChunkSizeInKB * 1024;
            if (Position % sizeOfBlock > 0)
            {
                long paddingSize = sizeOfBlock - (Position % sizeOfBlock);
                Padding = new byte[paddingSize];
                _ = stream.Read(Padding, 0, (int)paddingSize);
            }

            try
            {
                Certificate = new X509Certificate(Catalog);
            }
            catch { }

            Image = new ImageFlash(stream);

            DataBlocksPosition = (ulong)stream.Position;

            ParseBlockHashes();
        }

        private void ParseBlockHashes()
        {
            BlockHashes.Clear();

            uint sizeOfBlock = SecurityHeader.ChunkSizeInKB * 1024;
            uint ImageHeaderPosition = GetImageHeaderPosition();

            ulong numberOfDataBlocksToVerify = Image.GetDataBlockCount();
            ulong imageHeadersBlockCount = (DataBlocksPosition - ImageHeaderPosition) / sizeOfBlock;
            ulong numberOfBlocksToVerify = numberOfDataBlocksToVerify + imageHeadersBlockCount;

            ulong sizeOfHash = (ulong)TableOfHashes.LongLength / numberOfBlocksToVerify;

            for (int i = 0; i < TableOfHashes.Length; i += (int)sizeOfHash)
            {
                BlockHashes.Add(TableOfHashes[i..(i + (int)sizeOfHash)]);
            }
        }

        private uint GetImageHeaderPosition()
        {
            uint sizeOfBlock = SecurityHeader.ChunkSizeInKB * 1024;
            uint ImageHeaderPosition = SecurityHeader.Size + SecurityHeader.CatalogSize + SecurityHeader.HashTableSize;
            if (ImageHeaderPosition % sizeOfBlock > 0)
            {
                uint paddingSize = sizeOfBlock - (ImageHeaderPosition % sizeOfBlock);
                ImageHeaderPosition += paddingSize;
            }
            return ImageHeaderPosition;
        }

        public void VerifyFFU()
        {
            ulong numberOfBlocksToVerify = Image.GetImageBlockCount();

            long oldPosition = Stream.Position;

            using BinaryReader binaryReader = new(Stream, Encoding.ASCII, true);
            using BinaryReader hashTableBinaryReader = new(new MemoryStream(TableOfHashes));

            for (ulong i = 0; i < numberOfBlocksToVerify; i++)
            {
                Console.Title = $"{i}/{numberOfBlocksToVerify}";

                if (SecurityHeader.AlgorithmId == 0x0000800c) // SHA256 Algorithm ID
                {
                    byte[] block = Image.GetImageBlock(Stream, i);
                    byte[] hash = SHA256.HashData(block);
                    byte[] hashTableHash = BlockHashes.ElementAt((int)i);

                    if (!StructuralComparisons.StructuralEqualityComparer.Equals(hash, hashTableHash))
                    {
                        _ = Stream.Seek(oldPosition, SeekOrigin.Begin);
                        throw new InvalidDataException($"The FFU image contains a mismatched hash value at chunk {i}. " +
                            $"Expected: {BitConverter.ToString(hashTableHash).Replace("-", "")} " +
                            $"Computed: {BitConverter.ToString(hash).Replace("-", "")}");
                    }
                }
                else
                {
                    throw new InvalidDataException($"Unknown Hash algorithm id: {SecurityHeader.AlgorithmId}");
                }
            }

            Console.Title = $"{numberOfBlocksToVerify}/{numberOfBlocksToVerify}";

            _ = Stream.Seek(oldPosition, SeekOrigin.Begin);
        }
    }
}