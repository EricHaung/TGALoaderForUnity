using System;
using System.IO;
using UnityEngine;

public static class TGALoader
{
    public enum DataType
    {
        Empty, //image data included.
        U_CM,  //Uncompressed, color-mapped images.
        U_RGB, //Uncompressed, RGB images.
        U_BW,  //Uncompressed, black and white images.
        RLE_CM,  //Runlength encoded color-mapped images.
        RLE_RGB,  //Runlength encoded RGB images.
        C_BW,  //Compressed, black and white images.
        C_CM,  //Compressed color-mapped data, using Huffman, Delta, and runlength encoding.
        C_CM4  //Compressed color-mapped data, using Huffman, Delta, and runlength encoding.  4-pass quadtree-type process.
    }

    public static Texture2D LoadTGA(byte[] TGAStream)
    {
        DataType dataType = DataType.Empty;
        MemoryStream stream = new MemoryStream(TGAStream);
        using (BinaryReader r = new BinaryReader(stream))
        {
            r.BaseStream.Seek(2, SeekOrigin.Begin);
            dataType = GetDataType(r.ReadByte());
            r.BaseStream.Seek(9, SeekOrigin.Current);

            short width = r.ReadInt16();
            short height = r.ReadInt16();
            int bitDepth = r.ReadByte();

            r.BaseStream.Seek(1, SeekOrigin.Current);
            Texture2D tex = new Texture2D(width, height);
            Color32[] pulledColors = new Color32[width * height];

            if (dataType == DataType.RLE_RGB)
            {
                pulledColors = RLEDedoce(r, width, height, bitDepth);
            }
            else
            {
                for (int i = 0; i < width * height; i++)
                    pulledColors[i] = GetColor32(r, bitDepth);
            }

            tex.SetPixels32(pulledColors);
            tex.Apply();
            return tex;

        }
    }

    private static DataType GetDataType(int value)
    {
        switch (value)
        {
            case 0:
                return DataType.Empty;
            case 1:
                return DataType.U_CM;
            case 2:
                return DataType.U_RGB;
            case 3:
                return DataType.U_BW;
            case 9:
                return DataType.RLE_CM;
            case 10:
                return DataType.RLE_RGB;
            case 11:
                return DataType.C_BW;
            case 32:
                return DataType.C_CM;
            case 33:
                return DataType.C_CM4;
            default:
                return DataType.Empty;
        }
    }

    private static Color32[] RLEDedoce(BinaryReader r, short width, short height, int bitDepth)
    {
        Color32[] pulledColors = new Color32[width * height];
        Color32 sampleColor;
        byte header;
        int length = 0;
        int idx = 0;

        while (idx < width * height)
        {
            header = r.ReadByte();
            if ((header >> 7) == 1) //run-length packet
            {
                length = (header & 0x7F) + 1;
                sampleColor = GetColor32(r, bitDepth);

                for (int i = 0; i < length; i++)
                {
                    pulledColors[idx + i] = sampleColor;
                }
                idx += length;
            }
            else if ((header >> 7) == 0) //raw packet
            {
                length = (header & 0x7F) + 1;

                for (int i = 0; i < length; i++)
                {
                    pulledColors[idx + i] = GetColor32(r, bitDepth);
                }
                idx += length;
            }
            else
            {
                throw new Exception("TGA texture RLE Decode ERROR");
            }
        }


        return pulledColors;
    }

    private static Color32 GetColor32(BinaryReader r, int bitDepth)
    {
        Color32 color = new Color32();
        if (bitDepth == 32)
        {
            byte red = r.ReadByte();
            byte green = r.ReadByte();
            byte blue = r.ReadByte();
            byte alpha = r.ReadByte();

            color = new Color32(blue, green, red, alpha);
        }
        else if (bitDepth == 24)
        {
            byte red = r.ReadByte();
            byte green = r.ReadByte();
            byte blue = r.ReadByte();

            color = new Color32(blue, green, red, 255);
        }
        else if (bitDepth == 16) //ARRRRRGG GGGBBBBB
        {
            byte rvalue = r.ReadByte();
            byte lvalue = r.ReadByte();
            int alpha = (lvalue >> 7);
            int red = ((lvalue & 0x7F) >> 2);
            int green = ((lvalue & 0x03) << 3) + (rvalue >> 5);
            int blue = (rvalue & 0x1F);

            color = new Color(red / 32f, green / 32f, blue / 32f, 1);
        }
        else
        {
            throw new Exception("TGA texture had non 32/24/16 bit depth.");
        }

        return color;
    }
}
