﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace libhac
{
    public class Ticket
    {
        public TicketSigType SignatureType { get; }
        public byte[] Signature { get; }
        public string Issuer { get; }
        public byte[] TitleKeyBlock { get; }
        public TitleKeyType TitleKeyType { get; }
        public byte CryptoType { get; }
        public ulong TicketId { get; }
        public ulong DeviceId { get; }
        public byte[] RightsId { get; }
        public uint AccountId { get; }
        public int Length { get; } // Not completely sure about this one
        public byte[] File { get; }

        internal static readonly byte[] LabelHash =
        {
            0xE3, 0xB0, 0xC4, 0x42, 0x98, 0xFC, 0x1C, 0x14, 0x9A, 0xFB, 0xF4, 0xC8, 0x99, 0x6F, 0xB9, 0x24,
            0x27, 0xAE, 0x41, 0xE4, 0x64, 0x9B, 0x93, 0x4C, 0xA4, 0x95, 0x99, 0x1B, 0x78, 0x52, 0xB8, 0x55
        };

        public Ticket(BinaryReader reader)
        {
            var fileStart = reader.BaseStream.Position;
            SignatureType = (TicketSigType)reader.ReadUInt32();

            switch (SignatureType)
            {
                case TicketSigType.Rsa4096Sha1:
                case TicketSigType.Rsa4096Sha256:
                    Signature = reader.ReadBytes(0x200);
                    reader.BaseStream.Position += 0x3c;
                    break;
                case TicketSigType.Rsa2048Sha1:
                case TicketSigType.Rsa2048Sha256:
                    Signature = reader.ReadBytes(0x100);
                    reader.BaseStream.Position += 0x3c;
                    break;
                case TicketSigType.EcdsaSha1:
                case TicketSigType.EcdsaSha256:
                    Signature = reader.ReadBytes(0x3c);
                    reader.BaseStream.Position += 0x40;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var dataStart = reader.BaseStream.Position;

            Issuer = reader.ReadUtf8Z();
            reader.BaseStream.Position = dataStart + 0x40;
            TitleKeyBlock = reader.ReadBytes(0x100);
            reader.BaseStream.Position = dataStart + 0x141;
            TitleKeyType = (TitleKeyType)reader.ReadByte();
            reader.BaseStream.Position = dataStart + 0x145;
            CryptoType = reader.ReadByte();
            reader.BaseStream.Position = dataStart + 0x150;
            TicketId = reader.ReadUInt64();
            DeviceId = reader.ReadUInt64();
            RightsId = reader.ReadBytes(0x10);
            AccountId = reader.ReadUInt32();
            reader.BaseStream.Position = dataStart + 0x178;
            Length = reader.ReadInt32();

            reader.BaseStream.Position = fileStart;
            File = reader.ReadBytes(Length);
        }

        public static Ticket[] SearchTickets(Stream file, IProgressReport logger = null)
        {
            var reader = new BinaryReader(file, Encoding.Default, true);
            file.Position += 0x140;
            var tickets = new Dictionary<string, Ticket>();

            logger?.SetTotal(file.Length);

            // Ticket starts occur at multiples of 0x400
            while (file.Position + 0x800 < file.Length)
            {
                var position = file.Position;
                logger?.Report(position);

                if (reader.ReadUInt32() != 0x746f6f52) // Root
                {
                    file.Position = position + 0x400;
                    continue;
                }

                file.Position -= 0x144;
                var sigType = (TicketSigType)reader.ReadUInt32();
                if (sigType < TicketSigType.Rsa4096Sha1 || sigType > TicketSigType.EcdsaSha256)
                {
                    file.Position = position + 0x400;
                    continue;
                }

                file.Position -= 4;

                var ticket = new Ticket(reader);
                tickets[ticket.RightsId.ToHexString()] = ticket;
                file.Position = position + 0x400;
            }

            logger?.SetTotal(0);
            return tickets.Values.ToArray();
        }

        public byte[] GetTitleKey(Keyset keyset)
        {
            if (TitleKeyType == TitleKeyType.Common)
            {
                var commonKey = new byte[0x10];
                Array.Copy(TitleKeyBlock, commonKey, commonKey.Length);
                return commonKey;
            }

            return Crypto.DecryptTitleKey(TitleKeyBlock, keyset.eticket_ext_key_rsa);
        }
    }

    public enum TicketSigType
    {
        Rsa4096Sha1 = 0x10000,
        Rsa2048Sha1,
        EcdsaSha1,
        Rsa4096Sha256,
        Rsa2048Sha256,
        EcdsaSha256
    }

    public enum TitleKeyType
    {
        Common,
        Personalized
    }
}