using System.Drawing.Imaging;
namespace App20241217
{
    public partial class Form1 : Form
    {

        private Point leftPoint = Point.Empty;   // 左圖匹配點
        private Point rightPoint = Point.Empty;  // 右圖匹配點
        private const double FocalLength = 12.07; // 焦距 (mm)
        private const double PixelSize = 0.0033450704225352; // 每像素大小 (mm)
        private Bitmap templateImage; // 模板圖片
        private bool isLeftMatched = false, isRightMatched = false;

        public Form1()
        {
            InitializeComponent();



            // 載入模板圖片
            templateImage = new Bitmap(@"C:\模板圖.png");
        }



        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = "圖像文件(JPeg, Gif, Bmp, etc.)|.jpg;*jpeg;*.gif;*.bmp;*.tif;*.tiff;*.png|所有文件(*.*)|*.*";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Bitmap MyBitmap = new Bitmap(openFileDialog1.FileName);
                    this.pictureBox1.Image = MyBitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "訊息顯示");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = "圖像文件(JPeg, Gif, Bmp, etc.)|.jpg;*jpeg;*.gif;*.bmp;*.tif;*.tiff;*.png|所有文件(*.*)|*.*";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Bitmap MyBitmap = new Bitmap(openFileDialog1.FileName);
                    this.pictureBox2.Image = MyBitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "訊息顯示");
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            try
            {
                // 檢查是否有正在執行的模板匹配
                if (button3.Enabled == false)
                {
                    MessageBox.Show("模板匹配正在進行中，請稍後再試。", "提示");
                    return;
                }

                if (pictureBox1.Image != null && pictureBox2.Image != null)
                {
                    Bitmap targetImage1 = new Bitmap(pictureBox1.Image);
                    Bitmap targetImage2 = new Bitmap(pictureBox2.Image);

                    // 確保模板圖片已載入
                    if (templateImage == null)
                    {
                        MessageBox.Show("模板圖片未載入，請檢查模板檔案是否存在。", "錯誤");
                        return;
                    }

                    // 禁用按鈕，避免重複點擊
                    button3.Enabled = false;
                    label1.Text = "模板匹配中...";
                    label2.Text = "模板匹配中...";

                    // 使用異步方式執行模板匹配
                    (Point leftPointResult, Point rightPointResult) = await Task.Run(() =>
                    {
                        Point left = TemplateMatch(targetImage1, templateImage);
                        Point right = TemplateMatch(targetImage2, templateImage);
                        return (left, right);
                    });

                    // 更新 UI 顯示匹配結果
                    leftPoint = leftPointResult;
                    rightPoint = rightPointResult;
                    label1.Text = $"左圖紅點: ({leftPoint.X}, {leftPoint.Y})";
                    label2.Text = $"右圖紅點: ({rightPoint.X}, {rightPoint.Y})";

                    // 繪製紅點位置在圖片上
                    using (Graphics g1 = Graphics.FromImage(targetImage1))
                    {
                        g1.DrawEllipse(Pens.Red, leftPoint.X - 5, leftPoint.Y - 5, 10, 10);
                    }
                    using (Graphics g2 = Graphics.FromImage(targetImage2))
                    {
                        g2.DrawEllipse(Pens.Red, rightPoint.X - 5, rightPoint.Y - 5, 10, 10);
                    }

                    pictureBox1.Image = targetImage1;
                    pictureBox2.Image = targetImage2;

                    // 匹配完成提示
                    MessageBox.Show($"匹配完成！\n左圖紅點座標: ({leftPoint.X}, {leftPoint.Y})\n右圖紅點座標: ({rightPoint.X}, {rightPoint.Y})", "匹配結果");
                }
                else
                {
                    MessageBox.Show("請先載入兩張影像。", "錯誤");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "錯誤");
            }
            finally
            {
                // 啟用按鈕
                button3.Enabled = true;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (isLeftMatched && isRightMatched)
                {
                    if (double.TryParse(textBox1.Text, out double baselineCm))
                    {
                        double baselineMm = baselineCm * 10;
                        double disparityPixels = Math.Abs(leftPoint.X - rightPoint.X);
                        double disparityMm = disparityPixels * PixelSize;

                        if (disparityMm == 0)
                        {
                            MessageBox.Show("視差為零，無法計算深度。請檢查紅點選取是否正確。", "錯誤");
                            return;
                        }

                        double depthMm = (FocalLength * baselineMm) / disparityMm;
                        double depthCm = depthMm / 10;

                        label1.Text = $"深度: {depthCm:F2} cm";
                    }
                    else
                    {
                        MessageBox.Show("請輸入有效的相機基線距離。", "錯誤");
                    }
                }
                else
                {
                    MessageBox.Show("請先進行模板匹配。", "錯誤");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"深度計算時發生錯誤: {ex.Message}", "錯誤");
            }
        }

        // 模板匹配方法 (SAD 演算法)
        private Point TemplateMatch(Bitmap target, Bitmap template)
        {
            int tWidth = template.Width;
            int tHeight = template.Height;
            int bestX = 0, bestY = 0;
            double minSAD = double.MaxValue;
            int step = 2;  // 每次步進2個像素點來加速匹配

            // 計算圖像的合適範圍
            for (int x = 0; x <= target.Width - tWidth; x += step)
            {
                for (int y = 0; y <= target.Height - tHeight; y += step)
                {
                    double sad = CalculateSAD(target, template, x, y);
                    if (sad < minSAD)
                    {
                        minSAD = sad;
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            return new Point(bestX + tWidth / 2, bestY + tHeight / 2); // 返回中心座標
        }

        // 計算 SAD (絕對差值和)
        private double CalculateSAD(Bitmap target, Bitmap template, int startX, int startY)
        {
            double sad = 0;

            // 確保模板不會超出目標圖像邊界
            if (startX < 0 || startY < 0 || startX + template.Width > target.Width || startY + template.Height > target.Height)
            {
                throw new ArgumentOutOfRangeException("模板超出了目標圖像的邊界");
            }

            // 遍歷模板圖像像素
            for (int x = 0; x < template.Width; x++)
            {
                for (int y = 0; y < template.Height; y++)
                {
                    try
                    {
                        // 取得目標圖像和模板圖像的像素
                        Color targetPixel = target.GetPixel(startX + x, startY + y);
                        Color templatePixel = template.GetPixel(x, y);

                        // 計算 SAD（絕對差值和）
                        sad += Math.Abs(targetPixel.R - templatePixel.R) +
                               Math.Abs(targetPixel.G - templatePixel.G) +
                               Math.Abs(targetPixel.B - templatePixel.B);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"錯誤處理像素: {ex.Message}", "錯誤");
                        return double.MaxValue; // 當發生錯誤時，返回最大值，避免錯誤的匹配
                    }
                }
            }

            return sad;
        }

    }
}