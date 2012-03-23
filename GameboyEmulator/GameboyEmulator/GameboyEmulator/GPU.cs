﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GameboyEmulator
{
    public enum GPUMode
    {
        HBlankPeriod = 0x0,
        VBlankPeriod = 0x01,
        OAMReadMode = 0x02,
        VRAMReadMode = 0x03
    }
    
    public class GPU
    {
        private const int lcdLinesCount = 143;
        private const int hBlankCycleDuration = 204;
        private const int vBlankCycleDuration = 456;
        private const int oamRadModeCycleDuration = 80;
        private const int vRadModeCycleDuration = 172;

        private readonly Clock clock;
        private readonly Clock cpuClock;
        private readonly GPURegisters gpuRegisters;
        private GPUMode gpuMode;

        private int lineIndex;

        private readonly byte[] memoryData;
        private readonly byte[] tileSet;
        private readonly byte[] tileBackgroundMap;
        private readonly byte[] oamData;
        private readonly byte[] zRamData;

        private Bitmap bmp = new Bitmap(160, 144, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        public GPU( Clock cpuClock, GPURegisters gpuRegisters )
        {
            this.cpuClock = cpuClock;
            this.gpuRegisters = gpuRegisters;

            clock = new Clock();

            gpuMode = GPUMode.HBlankPeriod;
            lineIndex = 0;

            memoryData = new byte[0x2000];
            tileSet = new byte[0x17FF];
            tileBackgroundMap = new byte[0x7FF];
            oamData = new byte[0xA0];
            zRamData = new byte[0x7F];
        }

        public void FrameStep()
        {
            clock.IncrementCycleCount( cpuClock.LastCycleCountIncrement );

            switch (gpuMode)
            {
                case GPUMode.HBlankPeriod:
                    {
                        if (clock.CycleCount >= hBlankCycleDuration)
                        {
                            clock.Reset();
                            lineIndex++;

                            if (lineIndex == lcdLinesCount)
                            {
                                gpuMode = GPUMode.VBlankPeriod;
                                //TODO draw image
                            }
                            else
                            {
                                gpuMode = GPUMode.OAMReadMode;
                            }
                        }
                    }
                    break;
                case GPUMode.VBlankPeriod:
                    {
                        if (clock.CycleCount >= vBlankCycleDuration)
                        {
                            clock.Reset();
                            lineIndex++;

                            if (lineIndex > 153)
                            {
                                gpuMode = GPUMode.OAMReadMode;
                                lineIndex = 0;
                            }
                        }
                    }
                    break;
                case GPUMode.OAMReadMode:
                    {
                        if (clock.CycleCount >= oamRadModeCycleDuration)
                        {
                            gpuMode = GPUMode.VRAMReadMode;
                            clock.Reset();
                        }
                    }
                    break;
                case GPUMode.VRAMReadMode:
                    {
                        if (clock.CycleCount >= vRadModeCycleDuration)
                        {
                            gpuMode = GPUMode.HBlankPeriod;
                            clock.Reset();

                            RenderScan();
                        }
                    }
                    break;
            }
        }

        public byte ReadFromRAM( int offset )
        {
            // offset has already been substracted by 0x8000, so the end offset of the tile map is 0x97FF - 0x8000
            if ( offset < 0x17FF)
            {
                return tileSet[ offset ];
            }
            if (offset >= 0x1800 && offset < 0x1FFF)
            {
                return tileBackgroundMap[offset - 0x1800];
            }

            return memoryData[ offset ];
        }

        public void WriteInRAM( int offset, byte value )
        {
            // offset has already been substracted by 0x8000, so the end offset of the tile map is 0x97FF - 0x8000
            if ( offset < 0x17FF )
            {
                tileSet[ offset ] = value;
            }
            else if (offset >= 0x1800 && offset < 0x1FFF)
            {
                tileBackgroundMap[offset - 0x1800] = value;
            }

            memoryData[ offset ] = value;
        }

        public byte ReadFromOAM( int offset )
        {
            return oamData[ offset ];
        }

        public void WriteToOAM(int offset, byte value)
        {
            oamData[ offset ] = value;
        }

        public byte ReadFromZeroPageRAM( int offset )
        {
            return zRamData[ offset ];
        }

        public void WriteToZeroPageRAM( int offset, byte value )
        {
            zRamData[ offset ] = value;
        }

        private void RenderScan()
        {
            var tileMapOffset = gpuRegisters.BackgroundTileMap == 1 ? 0x1C00 : 0x1800;
            tileMapOffset += (gpuRegisters.CurrentScanLine + gpuRegisters.ScrollY) >> 3;

            var lineOffset = gpuRegisters.ScrollX >> 3;

            var y = (gpuRegisters.CurrentScanLine + gpuRegisters.ScrollY) & 7;
            var x = gpuRegisters.ScrollX & 7;

            var tile = (int)tileSet[ tileMapOffset + lineOffset - 0x1800];

            if (gpuRegisters.BackgroundTileSet == 1 && tile < 128)
            {
                tile += 256;
            }

            Color color;

            var bmpData = bmp.LockBits()

            for (var i = 0; i < 160; i++)
            {
                byte[] palette = gpuRegisters.GetPalette(tileBackgroundMap[tile + y + x]);

                color = Color.FromArgb(palette[3], palette[2], palette[1], palette[0] );

                bmp.SetPixel(0, gpuRegisters.CurrentScanLine, color);
            }
        }
    }
}
