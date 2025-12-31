using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Cornifer.Capture.PSD
{
    public class PSDFile
    {
        static readonly byte[] Magic8BPS = Encoding.ASCII.GetBytes("8BPS");
        static readonly byte[] Magic8BIM = Encoding.ASCII.GetBytes("8BIM");
        static readonly byte[] BlendNormal = Encoding.ASCII.GetBytes("norm");
		static readonly byte[] UnicodeName = Encoding.ASCII.GetBytes("luni");

        public uint Width, Height;
        public List<Layer> Layers = new();

        public void Write(Stream stream)
        {
            BigEndianWriter writer = new(stream);

			using TaskProgress mainprog = new("Writing PSD", 5);
			WriteHeader(writer);
			mainprog.Progress = 1;
			WriteColorModeData(writer);
			mainprog.Progress = 2;
			WriteImageResources(writer);
			mainprog.Progress = 3;
			WriteLayerMaskInfo(writer);
			mainprog.Progress = 4;
			WriteImageData(writer);
			mainprog.Progress = 5;
		}

        void WriteHeader(BigEndianWriter writer)
        {
            writer.Write(Magic8BPS);
            writer.Write((ushort)1); // Version
            writer.Write(0u);        // Reserved
            writer.Write((ushort)0); // Reserved
            writer.Write((ushort)4); // Channels
            writer.Write(Height);
            writer.Write(Width);
            writer.Write((ushort)8); // Bits per channel
            writer.Write((ushort)3); // Color mode, RGB
        }

        void WriteColorModeData(BigEndianWriter writer)
        {
            writer.Write((uint)0);   // Length
        }

        void WriteImageResources(BigEndianWriter writer)
        {
            writer.Write((uint)0);   // Length
        }

        void WriteLayerMaskInfo(BigEndianWriter writer)
        {
            SizedWrite(writer, false, writer =>		// Layer and mask information
            {
                SizedWrite(writer, true, writer =>	// Layer info
                {
                    writer.Write((ushort)Layers.Count);	// Typically 14									[2]

                    using MemoryStream channelImageStream = new();
                    BigEndianWriter channelImageWriter = new(channelImageStream);

                    using MemoryStream rleTemp = new();
                    using RleStream rle = new(rleTemp);

                    using (TaskProgress prog = new("Writing layer records", Layers.Count))
                    {
                        foreach (Layer layer in Layers) // Begin layer records
                        {
                            writer.Write(layer.Y);                  // Top								[4]			
							writer.Write(layer.X);                  // Left								[4]
							writer.Write(layer.Height + layer.Y);   // Bottom							[4]
							writer.Write(layer.Width + layer.X);	// Right							[4]

                            writer.Write((ushort)4);				// Number of channels (4)			[2]

                            int channelLength = WriteChannel(channelImageWriter, rleTemp, rle, layer, 0);
                            writer.Write((ushort)0);                // Channel type: Red				[2]
                            writer.Write((uint)channelLength);      // Channel data length				[4]

                            channelLength = WriteChannel(channelImageWriter, rleTemp, rle, layer, 1);
                            writer.Write((ushort)1);                // Channel type: Green				[2]
							writer.Write((uint)channelLength);      // Channel data length				[4]

							channelLength = WriteChannel(channelImageWriter, rleTemp, rle, layer, 2);
                            writer.Write((ushort)2);                // Channel type: Blue				[2]
							writer.Write((uint)channelLength);      // Channel data length				[4]

							channelLength = WriteChannel(channelImageWriter, rleTemp, rle, layer, 3);
                            writer.Write((short)-1);                // Channel type: Transparency mask	[2]
							writer.Write((uint)channelLength);      // Channel data length				[4]

							writer.Write(Magic8BIM);                // Blend mode signature '8BIM'		[4]
							writer.Write(BlendNormal);              // Blend mode key 'norm'			[4]
							writer.Write(layer.Opacity);			// Opacity							[1]
                            writer.Write((byte)0);					// Clipping (non-base)				[1]
                            writer.Write(layer.Visible ? (byte)0x00 : (byte)0x02); // Visibility flag	[1]
                            writer.Write((byte)0);					// Padding							[1]

                            SizedWrite(writer, false, writer =>		// Extra data field
                            {
                                writer.Write(0u);					// Layer mask length (0)			[4]

								writer.Write(40u);					// Layer mask length (0)			[4]
								writer.Write(0xFFFF);				// Gray blend source				[4]
								writer.Write(0xFFFF);				// Gray blend destination			[4]
								for (int j = 0; j < 4; j++) {
									writer.Write(0xFFFF);           // Channel j source range			[4]   
									writer.Write(0xFFFF);           // Channel j destination range		[4]
								}

								byte[] namebytes = Encoding.UTF8.GetBytes(layer.Name);
								writer.Write((byte)namebytes.Length); // first byte of UCSD string		[1]
                                writer.Write(namebytes);

								while (writer.BaseStream.Position % 4 != 0)
									writer.Write((byte)0);

								writer.Write((ushort)0);

								/*int pad = 4 - ((1 + namebytes.Length) % 4);
								for (int i = 0; i < pad; i++)
								    writer.Write((byte)0);*/
							});

							// End layer record

                            prog.Progress += 1;
                        }
                    }

                    channelImageStream.Position = 0;
                    channelImageStream.CopyTo(writer.BaseStream);	// Copy channel image data to end of layer records
                });

                /*writer.Write(14u);		// Global layer mask data length (14)						[4]
				writer.Write((ushort)0);    // Overlay colour space										[2]
				writer.Write(0L);           // 4 * 2 byte colour components								[8]
				writer.Write((ushort)0);    // Opacity (0)												[2]
				writer.Write((byte)1);      // Kind (Colour protected)									[1]
				writer.Write((byte)0);      // Filler													[1]

				writer.Write(Magic8BIM);
				writer.Write(UnicodeName);
				writer.Write(1u);
				writer.Write(0xFF00);*/
			});
        }

        int WriteChannel(BigEndianWriter writer, MemoryStream temp, RleStream rle, Layer layer, int c)
        {
            temp.Position = 0;
            temp.SetLength(0);

            long start = writer.BaseStream.Position;

            writer.Write((ushort)1); // RLE compression

            long rowStart = 0;
            int posCounter = c;
            for (int j = 0; j < layer.Height; j++)
            {
                for (int i = 0; i < layer.Width; i++)
                {
                    rle.WriteByte(layer.Data[posCounter]);
                    posCounter += 4;
                }
                rle.Flush();

                ushort rowLength = (ushort)(temp.Position - rowStart);
                writer.Write(rowLength);
                rowStart = temp.Position;
            }

            temp.Position = 0;
            temp.CopyTo(writer.BaseStream);

            int length = (int)(writer.BaseStream.Position - start);
            //if (length % 2 == 1)
            //{
            //    writer.Write((byte)0);
            //    length++;
            //}

            return length;
        }

        void WriteImageData(BigEndianWriter writer)
        {
            using MemoryStream temp = new();
            RleStream rle = new(temp);

            writer.Write((ushort)1);   // RLE compression

            temp.Position = 0;
            temp.SetLength(0);

            long rowStart = 0;
            for (int c = 0; c < 4; c++)
            {
                for (int j = 0; j < Height; j++)
                {
                    for (int i = 0; i < Width; i++)
                    {
                        rle.WriteByte(0);
                    }
                    rle.Flush();

                    ushort rowLength = (ushort)(temp.Position - rowStart);
                    writer.Write(rowLength);
                    rowStart = temp.Position;
                }

            }
            temp.Position = 0;
            temp.CopyTo(writer.BaseStream);

            //writer.Write((uint)0);
        }

        void SizedWrite(BigEndianWriter writer, bool roundEvenLength, Action<BigEndianWriter> callback)
        {
            uint length;

            if (!writer.BaseStream.CanSeek)
            {
                MemoryStream ms = new();
                BigEndianWriter mswriter = new(ms);
                callback(mswriter);

                length = (uint)ms.Length;
                if (length % 2 == 1 && roundEvenLength)
                    length++;

                writer.Write(length);
                ms.Position = 0;
                ms.CopyTo(writer.BaseStream);
                return;
            }

            long lenpos = writer.BaseStream.Position;
            writer.Write(0u);
            long datastart = writer.BaseStream.Position;
            callback(writer);
            long curpos = writer.BaseStream.Position;
            writer.BaseStream.Seek(lenpos, SeekOrigin.Begin);
            length = (uint)(curpos - datastart);

            bool roundLength = length % 2 == 1 && roundEvenLength;

            if (roundLength)
                length++;

            writer.Write(length);
            writer.BaseStream.Seek(curpos, SeekOrigin.Begin);

            if (roundLength)
                writer.Write((byte)0x00);
        }

        public struct Layer
        {
            public uint X;
            public uint Y;
            public uint Width;
            public uint Height;

            public bool Visible;
            public byte[] Data;
            public byte Opacity;
            public string Name;
        }
    }
}
