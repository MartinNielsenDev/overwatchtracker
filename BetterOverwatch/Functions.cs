﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using AForge.Imaging;
using AForge.Imaging.Filters;
using Image = System.Drawing.Image;

namespace BetterOverwatch
{
    class Functions
    {
        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")]
        public static extern uint SendMessage(IntPtr hWnd, uint msg, uint wParam, uint lParam);
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        public static string ActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return string.Empty;
        }
        public static Bitmap CaptureRegion(Bitmap frame, int x, int y, int width, int height)
        {
            return frame.Clone(new Rectangle(x, y, width, height), PixelFormat.Format32bppArgb);
        }
        public static Bitmap AdjustColors(Bitmap b, short radius, byte red = 255, byte green = 255, byte blue = 255, bool fillOutside = true)
        {
            EuclideanColorFiltering filter = new EuclideanColorFiltering
            {
                CenterColor = new RGB(red, green, blue),
                Radius = radius,
                FillOutside = fillOutside
            };
            if (!fillOutside)
            {
                filter.FillColor = new RGB(255, 255, 255);
            }
            filter.ApplyInPlace(b);

            return b;
        }
        public static void AdjustContrast(Bitmap image, float value, bool invertColors = false, bool limeToWhite = false)
        {
            BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
            int width = image.Width;
            int height = image.Height;
            int imageBytes = Image.GetPixelFormatSize(bitmapData.PixelFormat) / 8;

            unsafe
            {
                byte* rgb = (byte*)bitmapData.Scan0;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int pos = (y * bitmapData.Stride) + (x * imageBytes);
                        byte b = rgb[pos];
                        byte g = rgb[pos + 1];
                        byte r = rgb[pos + 2];

                        float red = r / 255.0f;
                        float green = g / 255.0f;
                        float blue = b / 255.0f;
                        red = (((red - 0.5f) * value) + 0.5f) * 255.0f;
                        green = (((green - 0.5f) * value) + 0.5f) * 255.0f;
                        blue = (((blue - 0.5f) * value) + 0.5f) * 255.0f;

                        int iR = (int)red;
                        iR = iR > 255 ? 255 : iR;
                        iR = iR < 0 ? 0 : iR;
                        int iG = (int)green;
                        iG = iG > 255 ? 255 : iG;
                        iG = iG < 0 ? 0 : iG;
                        int iB = (int)blue;
                        iB = iB > 255 ? 255 : iB;
                        iB = iB < 0 ? 0 : iB;

                        if (invertColors)
                        {
                            if (iB == 255 && iG == 255 && iR == 255)
                            {
                                iB = 0;
                                iG = 0;
                                iR = 0;
                            }
                            else
                            {
                                iB = 255;
                                iG = 255;
                                iR = 255;
                            }
                        }
                        if (limeToWhite && iG == 255 && iR == 255)
                        {
                            iB = 255;
                        }
                        rgb[pos] = (byte)iB;
                        rgb[pos + 1] = (byte)iG;
                        rgb[pos + 2] = (byte)iR;
                    }
                }
            }

            image.UnlockBits(bitmapData);
        }
        public static byte[] GetPixelAtPosition(Bitmap image, int pixelX, int pixelY)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            byte r, g, b;
            unsafe
            {
                byte* row = (byte*)data.Scan0 + (pixelY * data.Stride);
                b = row[pixelX * 4];
                g = row[(pixelX * 4) + 1];
                r = row[(pixelX * 4) + 2];
            }

            image.UnlockBits(data);

            return new[] { r, g, b };
        }
        public static double CompareTwoBitmaps(Bitmap image, Bitmap image2)
        {
            int correctPixels = 0;
            if (image.Width + image.Height != image2.Width + image2.Height) return 0.00;

            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            BitmapData data2 = image2.LockBits(new Rectangle(0, 0, image2.Width, image2.Height), ImageLockMode.ReadOnly, image2.PixelFormat);

            unsafe
            {
                for (int y = 0; y < image.Height; y++)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);
                    byte* row2 = (byte*)data2.Scan0 + (y * data2.Stride);

                    for (int x = 0; x < image.Width; x++)
                    {
                        byte b = row[x * 4];
                        byte g = row[(x * 4) + 1];
                        byte r = row[(x * 4) + 2];
                        byte b2 = row2[x * 4];
                        byte g2 = row2[(x * 4) + 1];
                        byte r2 = row2[(x * 4) + 2];

                        if (b == b2 && g == g2 && r == r2) correctPixels++;
                    }
                }
            }

            image.UnlockBits(data);
            image2.UnlockBits(data);

            return (correctPixels / (double)(image.Width * image.Height));

        }
        public static bool BitmapIsCertainColor(Bitmap image, int red, int green, int blue)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);

            unsafe
            {
                for (int y = 0; y < image.Height; y++)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);

                    for (int x = 0; x < image.Width; x++)
                    {
                        int b = row[x * 4];
                        int g = row[(x * 4) + 1];
                        int r = row[(x * 4) + 2];

                        if (blue - b > 12 || green - g > 12 || red - r > 12)
                            return false;
                    }
                }
            }
            image.UnlockBits(data);

            return true;
        }
        public static void InvertColors(Bitmap image)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
            unsafe
            {
                for (int y = 0; y < image.Height; y++)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);

                    for (int x = 0; x < image.Width; x++)
                    {
                        byte b = row[x * 4];
                        byte g = row[(x * 4) + 1];
                        byte r = row[(x * 4) + 2];
                        row[x * 4] = b == 255 ? (byte)0 : (byte)255;
                        row[(x * 4) + 1] = g == 255 ? (byte)0 : (byte)255;
                        row[(x * 4) + 2] = r == 255 ? (byte)0 : (byte)255;
                    }
                }
            }

            image.UnlockBits(data);
        }
        public static double CompareStrings(string string1, string string2)
        {
            try
            {
                string1 = string1.ToLower();
                string2 = string2.ToLower();
                int[,] d = new int[string1.Length + 1, string2.Length + 1];

                if (string1.Length == 0) return string2.Length;
                if (string2.Length == 0) return string1.Length;
                for (int i = 0; i <= string1.Length; d[i, 0] = i++) { }
                for (int j = 0; j <= string2.Length; d[0, j] = j++) { }

                for (int i = 1; i <= string1.Length; i++)
                {
                    for (int j = 1; j <= string2.Length; j++)
                    {
                        int cost = (string2[j - 1] == string1[i - 1]) ? 0 : 1;

                        d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                    }
                }
                int big = Math.Max(string1.Length, string2.Length);

                return Math.Floor(Convert.ToDouble(big - d[string1.Length, string2.Length]) / Convert.ToDouble(big) * 100);
            }
            catch (Exception e) { Console.WriteLine($"CompareStrings error: {e}"); }
            return 0.00;
        }
        public static string CheckMaps(string input)
        {
            for (int i = 0; i < Constants.mapList.Length; i++)
            {
                string mapName = Constants.mapList[i].Replace(" ", string.Empty).ToLower();

                if (input.ToLower().Contains(mapName)) return Constants.mapList[i];
            }
            for (int i = 0; i < Constants.mapList.Length; i++)
            {
                string mapName = Constants.mapList[i].Replace(" ", string.Empty).ToLower();

                if (CompareStrings(input, mapName) >= 60) return Constants.mapList[i];
            }
            return string.Empty;
        }
        public static bool CheckStats(int time, int eliminations, int damage, int objKills, int healing, int deaths)
        {
            int seconds = Convert.ToInt32(Math.Floor((double)time / 1000));

            if ((eliminations / seconds) * 60 < 7 &&
                (damage / seconds) * 60 < 2000 &&
                (objKills / seconds) * 60 < 7 &&
                (healing / seconds) * 60 < 2000 &&
                    (deaths / seconds) * 60 < 7)
            {
                return true;
            }
            return false;
        }
        public static string BitmapToText(Bitmap frame, int x, int y, int width, int height, bool contrastFirst = false, short radius = 110, Network network = 0, bool invertColors = false, byte red = 255, byte green = 255, byte blue = 255, bool fillOutside = true, bool limeToWhite = false)
        {
            string output = string.Empty;
            try
            {
                Bitmap result = frame.Clone(new Rectangle(x, y, width, height), PixelFormat.Format24bppRgb);

                if (contrastFirst)
                {
                    AdjustContrast(result, 255f, invertColors, limeToWhite);
                    result = AdjustColors(result, radius, red, green, blue, fillOutside);
                }
                else
                {
                    result = AdjustColors(result, radius, red, green, blue, fillOutside);
                    AdjustContrast(result, 255f, invertColors, limeToWhite);
                }

                output = FetchTextFromImage(result, network);
                result.Dispose();
            }
            catch (Exception e) { Console.WriteLine($"BitmapToText error: {e}"); }
            return output;
        }
        public static int[,] LabelImage(Bitmap image, out int labelCount)
        {
            BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            int nrow = image.Height;
            int ncol = image.Width;
            int[,] img = new int[nrow, ncol];
            int[,] label = new int[nrow, ncol];
            int imageBytes = Image.GetPixelFormatSize(bitmapData.PixelFormat) / 8;

            unsafe
            {
                byte* rgb = (byte*)bitmapData.Scan0;

                for (int x = 0; x < bitmapData.Width; x++)
                {
                    for (int y = 0; y < bitmapData.Height; y++)
                    {
                        int pos = (y * bitmapData.Stride) + (x * imageBytes);
                        if (rgb != null)
                        {
                            img[y, x] = (rgb[pos] + rgb[pos + 1] + rgb[pos + 2]) / 3;
                            label[y, x] = 0;
                        }
                    }
                }
            }
            image.UnlockBits(bitmapData);

            int lab = 1;
            Stack<int[]> stack = new Stack<int[]>();

            try
            {
                for (int c = 0; c != ncol; c++)
                {
                    for (int r = 0; r != nrow; r++)
                    {
                        if (img[r, c] == 0 || label[r, c] != 0)
                        {
                            continue;
                        }

                        stack.Push(new[] { r, c });
                        label[r, c] = lab;

                        while (stack.Count != 0)
                        {
                            int[] pos = stack.Pop();
                            int y = pos[0];
                            int x = pos[1];

                            if (y > 0 && x > 0)
                            {
                                if (img[y - 1, x - 1] > 0 && label[y - 1, x - 1] == 0)
                                {
                                    stack.Push(new[] { y - 1, x - 1 });
                                    label[y - 1, x - 1] = lab;
                                }
                            }

                            if (y > 0)
                            {
                                if (img[y - 1, x] > 0 && label[y - 1, x] == 0)
                                {
                                    stack.Push(new[] { y - 1, x });
                                    label[y - 1, x] = lab;
                                }
                            }

                            if (y > 0 && x < ncol - 1)
                            {
                                if (img[y - 1, x + 1] > 0 && label[y - 1, x + 1] == 0)
                                {
                                    stack.Push(new[] { y - 1, x + 1 });
                                    label[y - 1, x + 1] = lab;
                                }
                            }

                            if (x > 0)
                            {
                                if (img[y, x - 1] > 0 && label[y, x - 1] == 0)
                                {
                                    stack.Push(new[] { y, x - 1 });
                                    label[y, x - 1] = lab;
                                }
                            }

                            if (x < ncol - 1)
                            {
                                if (img[y, x + 1] > 0 && label[y, x + 1] == 0)
                                {
                                    stack.Push(new[] { y, x + 1 });
                                    label[y, x + 1] = lab;
                                }
                            }

                            if (y < nrow - 1 && x > 0)
                            {
                                if (img[y + 1, x - 1] > 0 && label[y + 1, x - 1] == 0)
                                {
                                    stack.Push(new[] { y + 1, x - 1 });
                                    label[y + 1, x - 1] = lab;
                                }
                            }

                            if (y < nrow - 1)
                            {
                                if (img[y + 1, x] > 0 && label[y + 1, x] == 0)
                                {
                                    stack.Push(new[] { y + 1, x });
                                    label[y + 1, x] = lab;
                                }
                            }

                            if (y < nrow - 1 && x < ncol - 1)
                            {
                                if (x + 1 == 21 && y + 1 == 15)
                                {
                                }
                                if (img[y + 1, x + 1] > 0 && label[y + 1, x + 1] == 0)
                                {
                                    stack.Push(new[] { y + 1, x + 1 });
                                    label[y + 1, x + 1] = lab;
                                }
                            }
                        }
                        lab++;
                    }
                }
            }
            catch { }
            labelCount = lab;

            return label;
        }
        public static Bitmap[] GetConnectedComponentLabels(Bitmap image)
        {
            int[,] labels = LabelImage(image, out int labelCount);
            List<Bitmap> bitmaps = new List<Bitmap>();

            if (labelCount > 0)
            {
                Rectangle[] rects = new Rectangle[labelCount];

                for (int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int i = 1; i < labelCount; i++)
                        {
                            if (labels[y, x] == i)
                            {
                                if (x < rects[i].X || rects[i].X == 0)
                                {
                                    rects[i].X = x;
                                }
                                if (y < rects[i].Y || rects[i].Y == 0)
                                {
                                    rects[i].Y = y;
                                }
                                if (x > rects[i].Width)
                                {
                                    rects[i].Width = x;
                                }
                                if (y > rects[i].Height)
                                {
                                    rects[i].Height = y;
                                }
                            }
                        }
                    }
                }

                for (int i = 1; i < labelCount; i++)
                {
                    int width = (rects[i].Width - rects[i].X) + 1;
                    int height = (rects[i].Height - rects[i].Y) + 1;

                    if (height / (double)image.Height > 0.6)
                    {
                        bitmaps.Add(new Bitmap(width, height, image.PixelFormat));

                        BitmapData bitmapData = bitmaps[bitmaps.Count - 1].LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, image.PixelFormat);
                        int imageBytes = Image.GetPixelFormatSize(bitmapData.PixelFormat) / 8;

                        unsafe
                        {
                            byte* rgb = (byte*)bitmapData.Scan0;

                            for (int x = 0; x < width; x++)
                            {
                                for (int y = 0; y < height; y++)
                                {
                                    int pos = (y * bitmapData.Stride) + (x * imageBytes);

                                    if (labels[(y + rects[i].Y), (x + rects[i].X)] == i)
                                    {
                                        if (rgb != null)
                                        {
                                            rgb[pos] = 255;
                                            rgb[pos + 1] = 255;
                                            rgb[pos + 2] = 255;
                                        }
                                    }
                                }
                            }
                            bitmaps[bitmaps.Count - 1].UnlockBits(bitmapData);
                        }
                    }
                }
            }

            return bitmaps.ToArray();
        }
        public static bool IsProcessOpen(string name)
        {
            if (Process.GetProcessesByName(name).Length > 0) return true;
            return false;
        }
        public static byte[] ImageToBytes(Image img)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
        public static Bitmap ReduceImageSize(Bitmap imgPhoto, int percent)
        {
            float nPercent = ((float)percent / 100);

            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            const int sourceX = 0;
            const int sourceY = 0;

            const int destX = 0;
            const int destY = 0;
            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(destWidth, destHeight, PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.InterpolationMode = InterpolationMode.High;
            grPhoto.DrawImage(imgPhoto, new Rectangle(destX, destY, destWidth, destHeight), new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight), GraphicsUnit.Pixel);
            grPhoto.Dispose();

            return bmPhoto;
        }
        public static string FetchLetterFromImage(BackPropNetwork network, Bitmap image, Network networkId)
        {
            double[] input = BackPropNetwork.CharToDoubleArray(image);

            for (int i = 0; i < network.InputNodesCount; i++)
            {
                network.InputNode(i).Value = input[i];
            }
            network.Run();
            int bestNode = network.BestNodeIndex;

            if (networkId == Network.Maps || networkId == Network.HeroNames)
            {
                return Convert.ToChar('A' + bestNode).ToString();
            }
            if (networkId == Network.TeamSkillRating || networkId == Network.Ratings || networkId == Network.Numbers)
            {
                return bestNode.ToString();
            }
            if (networkId == Network.PlayerNames)
            {
                return bestNode < 9 ? (bestNode + 1).ToString() : Convert.ToChar('A' + (bestNode - 9)).ToString();
            }
            return string.Empty;
        }
        public static string FetchTextFromImage(Bitmap image, Network network)
        {
            string text = string.Empty;
            try
            {
                Bitmap[] bitmaps = GetConnectedComponentLabels(image);
                text = AppData.tf.Run(bitmaps);
                Console.WriteLine("test:" + text);
                //for (int i = 0; i < bitmaps.Count; i++)
                //{
                //    if (network == Network.Maps)
                //    {
                //        text += FetchLetterFromImage(BetterOverwatchNetworks.mapsNN, bitmaps[i], network);
                //    }
                //    else if (network == Network.TeamSkillRating)
                //    {
                //        text += FetchLetterFromImage(BetterOverwatchNetworks.teamSkillRatingNN, bitmaps[i], network);
                //    }
                //    else if (network == Network.Ratings)
                //    {
                //        text += FetchLetterFromImage(BetterOverwatchNetworks.ratingsNN, bitmaps[i], network);
                //    }
                //    else if (network == Network.Numbers)
                //    {
                //        text += FetchLetterFromImage(BetterOverwatchNetworks.numbersNN, bitmaps[i], network);
                //    }
                //    else if (network == Network.HeroNames)
                //    {
                //        text += FetchLetterFromImage(BetterOverwatchNetworks.heroNamesNN, bitmaps[i], network);
                //    }
                //    else if (network == Network.PlayerNames)
                //    {
                //        text += FetchLetterFromImage(BetterOverwatchNetworks.playersNN, bitmaps[i], network);
                //    }
                //    if(text == "0")
                //    {
                //        //bitmaps[i].Save("C:/test/" + Guid.NewGuid() + ".png", ImageFormat.Bmp);
                //    }

                //    bitmaps[i].Dispose(); 
                //}
            }
            catch (Exception e)
            {
                DebugMessage("getTextFromImage() error: " + e);
            }
            return text;
        }
        public static void SetVolume(int vol)
        {
            int newVolume = ((ushort.MaxValue / 100) * vol);
            uint newVolumeAllChannels = (((uint)newVolume & 0x0000ffff) | ((uint)newVolume << 16));
            waveOutSetVolume(IntPtr.Zero, newVolumeAllChannels);
        }
        public static void DebugMessage(string msg)
        {
            try
            {
                if (Directory.Exists(AppData.configPath))
                {
                    string date = DateTime.Now.ToString("dd/MM/yy HH:mm:ss");
                    File.AppendAllText(Path.Combine(AppData.configPath, "debug.log"), $"[{date}] {msg + "\r\n"}");
                }
            }
            catch { }
            Debug.WriteLine(msg);
        }
        /*
 * UNUSED METHODS
         private static StringBuilder GetHashFromImage(Bitmap bitmap) // DEBUG
        {
            byte[] bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png); // gif for example
                bytes = ms.ToArray();
            }
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(bytes);

            // make a hex string of the hash for display or whatever
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2").ToLower());
            }

            return sb;
        }
private static Bitmap Downscale(Image original)
{
    double widthPercent = (double)original.Width / 1920 * 1366;
    double heightPercent = (double)original.Height / 1080 * 768;
    int width = (int)widthPercent;
    int height = (int)heightPercent;
    Bitmap downScaled = new Bitmap(width, height);

    using (Graphics graphics = Graphics.FromImage(downScaled))
    {
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(original, 0, 0, downScaled.Width, downScaled.Height);
    }

    return downScaled;
}

private static Bitmap Upscale(Image original)
{
    double widthPercent = (double)original.Width / 1366 * 1920;
    double heightPercent = (double)original.Height / 768 * 1080;
    int width = (int)widthPercent + 1;
    int height = (int)heightPercent + 1;
    Bitmap upScaled = new Bitmap(width, height);

    using (Graphics graphics = Graphics.FromImage(upScaled))
    {
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(original, 0, 0, upScaled.Width, upScaled.Height);
    }

    return upScaled;
}
*/
    }
}