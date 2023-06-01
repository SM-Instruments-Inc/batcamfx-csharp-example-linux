using OpenCvSharp;

namespace batcamfx_csharp_example_linux.Interpolator;

public class BeamformingInterpolator {
    /// <summary>
    /// dB less than this value is substituted with zero for visualization.
    /// </summary>
    private float _threshold;
    /// <summary>
    /// The value determines the perimeter to be left from maximum value of each frame.
    /// </summary>
    private float _range;
    /// <summary>
    /// Destination target for resizing 40x30 BF data array.
    /// </summary>
    private readonly Size _dSize;
    
    public BeamformingInterpolator(Size targetSize) {
        _threshold = 40f;
        _range = 3;
        _dSize = targetSize;
    }

    public void SetThreshold(double threshold) {
        _threshold = (float)threshold;
    }

    public void SetRange(double range) {
        _range = (float)range;
    }

    /// <summary>
    /// Creates an OpenCV matrix with BF Data received from BATCAM FX.
    ///
    /// This function changes BF Data into dB Data.
    /// The code converts each list with LINQ functions,
    /// but you can change method if you want to.
    ///
    /// Creating 2D Matrix, then resize it to desired size,
    /// and substitute each value to zero using lower limit and upper limit.
    ///
    /// For detailed information, please refer to received document.
    /// </summary>
    /// <param name="bytes">The raw bf data sent from BATCAM FX.</param>
    /// <param name="gain">Microphone gain value set in BATCAM FX.</param>
    /// <returns>The calculated OpenCV 2D Matrix.</returns>
    public Mat GenerateMatrix(float[] bytes, int gain) {
        var dBScale = bytes
            .Select(x => ConvertRawTodBScale(x, gain));
        var dB = dBScale
            .Select(x => (float)ConvertdBScaleTodB(x));

        var dBArray = dB as float[] ?? dB.ToArray();
        var maxValue = dBArray.Max();
        var matrix = new Mat(30, 40, MatType.CV_32FC1, dBArray);
        
        // You can change interpolation method if you see wierd image on result. 
        Cv2.Resize(matrix, matrix, _dSize, 0, 0, InterpolationFlags.Lanczos4);

        // The value only between upperLimit and lowerLimit will be left.
        var upperLimit = maxValue - _range;
        var lowerLimit = _threshold;

        if (Math.Abs(_range) * 100000 <= Math.Min(Math.Abs(_range + 1), 1)) {
            Cv2.Threshold(matrix, matrix, lowerLimit, 0, ThresholdTypes.Tozero);
        } else if (upperLimit > lowerLimit) {
            Cv2.Threshold(matrix, matrix, upperLimit, 0, ThresholdTypes.Tozero);
        } else {
            Cv2.Threshold(matrix, matrix, lowerLimit, 0, ThresholdTypes.Tozero);
        }

        var mask = matrix.GreaterThan(0);
        Cv2.Normalize(matrix, matrix, 255, 1, NormTypes.MinMax, -1, mask);
        matrix.ConvertTo(matrix, MatType.CV_8UC1);
        
        return matrix;
    }

    /// <summary>
    /// Convert Raw BF data into dB Scale data.
    /// This needs gain for correcting each value.
    /// </summary>
    /// <param name="rawData">The raw bf data sent from BATCAM FX.</param>
    /// <param name="gain">Microphone gain value set in BATCAM FX.</param>
    /// <returns></returns>
    private float ConvertRawTodBScale(float rawData, int gain) {
        gain = gain == 0 ? 1 : gain; // Prevents gain calculation with zero
        return rawData / (gain * gain) * 0.00031921f;
    }

    /// <summary>
    /// Convert dB Scale data into dB Data.
    /// </summary>
    /// <param name="dBScale">The dB scale data converted from `ConvertRawTodBScale`</param>
    /// <returns></returns>
    private double ConvertdBScaleTodB(float dBScale) {
        if (dBScale <= 0)
        {
            return 0; 
        }
        return 10 * Math.Log10(dBScale / 0.0000000004);
    }
}