using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public class LinearRegression
{
    private float[,] weights;
    private float[] biases;
    private int inputSize;
    private int outputSize;
    private float learningRate = 0.03f;

    public float LastR2 { get; private set; }
    public float LastMAE { get; private set; }
    public float LastMSE { get; private set; }

    public LinearRegression(int inputs, int outputs)
    {
        inputSize = inputs;
        outputSize = outputs;
        weights = new float[inputs, outputs];
        biases = new float[outputs];

        System.Random rng = new System.Random(42);
        for (int i = 0; i < inputs; i++)
            for (int j = 0; j < outputs; j++)
                weights[i, j] =
                    (float)(rng.NextDouble() * 0.02 - 0.01);
    }

    public void Train(
        float[][] x, float[][] y, int epochs = 1400)
    {
        int n = x.Length;
        if (n == 0) return;

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            float[,] weightGrad =
                new float[inputSize, outputSize];
            float[] biasGrad = new float[outputSize];

            for (int row = 0; row < n; row++)
            {
                float[] pred = PredictSingle(x[row]);

                for (int output = 0; output < outputSize; output++)
                {
                    float error = pred[output] - y[row][output];

                    for (int input = 0; input < inputSize; input++)
                        weightGrad[input, output] +=
                            error * x[row][input] / n;

                    biasGrad[output] += error / n;
                }
            }

            for (int input = 0; input < inputSize; input++)
                for (int output = 0; output < outputSize; output++)
                    weights[input, output] -=
                        learningRate * weightGrad[input, output];

            for (int output = 0; output < outputSize; output++)
                biases[output] -=
                    learningRate * biasGrad[output];
        }

        LastR2 = CalculateR2(x, y);
        LastMAE = CalculateMAE(x, y);
        LastMSE = CalculateMSE(x, y);

        Debug.Log(
            "ML training complete. " +
            $"R2={LastR2:F3}, " +
            $"MAE={LastMAE:F3}, " +
            $"MSE={LastMSE:F3}");
    }

    public float[] PredictSingle(float[] input)
    {
        float[] output = new float[outputSize];

        for (int outputIndex = 0;
             outputIndex < outputSize;
             outputIndex++)
        {
            output[outputIndex] = biases[outputIndex];

            for (int inputIndex = 0;
                 inputIndex < inputSize;
                 inputIndex++)
            {
                output[outputIndex] +=
                    input[inputIndex] *
                    weights[inputIndex, outputIndex];
            }
        }

        return output;
    }

    public float Predict(float[] input, int outputIndex)
    {
        return PredictSingle(input)[outputIndex];
    }

    public float CalculateR2(
        float[][] x, float[][] y)
    {
        if (x.Length == 0) return 0f;

        float totalSS = 0f;
        float residualSS = 0f;
        float[] means = new float[outputSize];

        for (int row = 0; row < x.Length; row++)
            for (int output = 0; output < outputSize; output++)
                means[output] += y[row][output] / x.Length;

        for (int row = 0; row < x.Length; row++)
        {
            float[] pred = PredictSingle(x[row]);

            for (int output = 0; output < outputSize; output++)
            {
                totalSS +=
                    Mathf.Pow(y[row][output] - means[output], 2);
                residualSS +=
                    Mathf.Pow(y[row][output] - pred[output], 2);
            }
        }

        if (totalSS <= 0.0001f) return 0f;
        return 1f - residualSS / totalSS;
    }

    public float CalculateMAE(
        float[][] x, float[][] y)
    {
        if (x.Length == 0) return 0f;

        float totalAbsError = 0f;
        int count = 0;

        for (int row = 0; row < x.Length; row++)
        {
            float[] pred = PredictSingle(x[row]);

            for (int output = 0; output < outputSize; output++)
            {
                totalAbsError +=
                    Mathf.Abs(y[row][output] - pred[output]);
                count++;
            }
        }

        return count == 0 ? 0f : totalAbsError / count;
    }

    public float CalculateMSE(
        float[][] x, float[][] y)
    {
        if (x.Length == 0) return 0f;

        float totalSquaredError = 0f;
        int count = 0;

        for (int row = 0; row < x.Length; row++)
        {
            float[] pred = PredictSingle(x[row]);

            for (int output = 0; output < outputSize; output++)
            {
                float error = y[row][output] - pred[output];
                totalSquaredError += error * error;
                count++;
            }
        }

        return count == 0 ? 0f : totalSquaredError / count;
    }

    public void SaveModel(string path)
    {
        List<string> lines = new List<string>();
        lines.Add($"{inputSize},{outputSize}");

        for (int input = 0; input < inputSize; input++)
            for (int output = 0; output < outputSize; output++)
                lines.Add(weights[input, output].ToString(
                    "F6",
                    CultureInfo.InvariantCulture));

        for (int output = 0; output < outputSize; output++)
            lines.Add(biases[output].ToString(
                "F6",
                CultureInfo.InvariantCulture));

        File.WriteAllLines(path, lines);
        Debug.Log("ML model saved: " + path);
    }

    public bool LoadModel(string path)
    {
        if (!File.Exists(path)) return false;

        string[] lines = File.ReadAllLines(path);
        if (lines.Length == 0) return false;

        string[] header = lines[0].Split(',');
        if (header.Length != 2 ||
            !int.TryParse(header[0], out int savedInputs) ||
            !int.TryParse(header[1], out int savedOutputs) ||
            savedInputs != inputSize ||
            savedOutputs != outputSize)
        {
            Debug.Log("ML model shape changed. Retraining from CSV.");
            return false;
        }

        int expectedLines = 1 + inputSize * outputSize + outputSize;
        if (lines.Length < expectedLines) return false;

        int idx = 1;

        for (int input = 0; input < inputSize; input++)
            for (int output = 0; output < outputSize; output++)
                float.TryParse(
                    lines[idx++],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out weights[input, output]);

        for (int output = 0; output < outputSize; output++)
            float.TryParse(
                lines[idx++],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out biases[output]);

        Debug.Log("ML model loaded: " + path);
        return true;
    }

    public void TrainFromCSV(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Debug.Log("ML CSV not found.");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 3)
        {
            Debug.Log($"Not enough ML data: {lines.Length - 1} rows.");
            return;
        }

        List<float[]> x = new List<float[]>();
        List<float[]> y = new List<float[]>();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] p = SplitCsvLine(lines[i]);
            if (p.Length < 12) continue;

            bool hasResultColumns = p.Length >= 14;
            float runCompleted = 0f;
            float performanceScore = 0f;

            int offset = 0;
            if (hasResultColumns)
            {
                runCompleted = p[1] == "Complete" ? 1f : 0f;
                performanceScore = Parse(p[2]);
                offset = 2;
            }

            float classCode = Parse(p[1 + offset]);
            float meleeAbW = Parse(p[2 + offset]);
            float rangedAbW = Parse(p[3 + offset]);
            float magicAbW = Parse(p[4 + offset]);
            float meleeRate = Parse(p[5 + offset]);
            float rangedRate = Parse(p[6 + offset]);
            float magicRate = Parse(p[7 + offset]);
            float deaths = Parse(p[8 + offset]);
            float hpLoot = Parse(p[9 + offset]);
            float manaLoot = Parse(p[10 + offset]);
            float coinLoot = Parse(p[11 + offset]);

            float[] input = BuildInput(
                classCode,
                meleeAbW,
                rangedAbW,
                magicAbW,
                meleeRate,
                rangedRate,
                magicRate,
                deaths,
                hpLoot,
                manaLoot,
                coinLoot,
                runCompleted,
                performanceScore);

            float[] output = BuildTargets(
                classCode,
                meleeAbW,
                rangedAbW,
                magicAbW,
                meleeRate,
                rangedRate,
                magicRate,
                deaths,
                hpLoot,
                manaLoot,
                coinLoot,
                runCompleted,
                performanceScore);

            x.Add(input);
            y.Add(output);
        }

        if (x.Count == 0) return;

        Train(x.ToArray(), y.ToArray());

        string modelPath =
            Application.persistentDataPath
            + "/adaptive_model_v2.txt";
        SaveModel(modelPath);
    }

    public static float[] BuildInput(
        float classCode,
        float meleeAbW,
        float rangedAbW,
        float magicAbW,
        float meleeRate,
        float rangedRate,
        float magicRate,
        float deaths,
        float hpLoot,
        float manaLoot,
        float coinLoot,
        float runCompleted = 0f,
        float performanceScore = 0f)
    {
        float isWarrior = Mathf.Approximately(classCode, 1f) ? 1f : 0f;
        float isMage = Mathf.Approximately(classCode, 2f) ? 1f : 0f;
        float isRogue = Mathf.Approximately(classCode, 3f) ? 1f : 0f;

        return new float[]
        {
            isWarrior,
            isMage,
            isRogue,
            Mathf.Clamp(meleeAbW / 3f, -1f, 1f),
            Mathf.Clamp(rangedAbW / 3f, -1f, 1f),
            Mathf.Clamp(magicAbW / 3f, -1f, 1f),
            Mathf.Clamp01(meleeRate),
            Mathf.Clamp01(rangedRate),
            Mathf.Clamp01(magicRate),
            Mathf.Clamp01(deaths / 5f),
            Mathf.Clamp01(hpLoot / 5f),
            Mathf.Clamp01(manaLoot / 5f),
            Mathf.Clamp01(coinLoot / 10f),
            Mathf.Clamp01(runCompleted),
            Mathf.Clamp01(performanceScore / 20f)
        };
    }

    private float[] BuildTargets(
        float classCode,
        float meleeAbW,
        float rangedAbW,
        float magicAbW,
        float meleeRate,
        float rangedRate,
        float magicRate,
        float deaths,
        float hpLoot,
        float manaLoot,
        float coinLoot,
        float runCompleted = 0f,
        float performanceScore = 0f)
    {
        float deathPressure = Mathf.Clamp01(deaths / 4f);
        float successPressure = Mathf.Clamp01(performanceScore / 20f);
        float meleeStyle = Mathf.Clamp01(
            meleeRate + Mathf.Max(0f, meleeAbW) / 3f);
        float rangedStyle = Mathf.Clamp01(
            rangedRate + Mathf.Max(0f, rangedAbW) / 3f);
        float magicStyle = Mathf.Clamp01(
            magicRate + Mathf.Max(0f, magicAbW) / 3f);
        float lootNeed = Mathf.Clamp01(
            hpLoot / 5f + manaLoot / 6f);
        float completedBonus = runCompleted >= 0.5f ? 1f : 0f;
        float perfectRunBonus = completedBonus > 0f && deaths <= 0f
            ? 1f
            : 0f;
        float stableRunBonus = completedBonus > 0f && deaths <= 1f
            ? 0.5f
            : 0f;

        float targetFill = 47f;
        targetFill += meleeStyle * 5f;
        targetFill -= rangedStyle * 2.5f;
        targetFill -= magicStyle * 4f;
        targetFill -= deathPressure * 6f;
        targetFill += successPressure * 1.5f;

        if (classCode == 1f) targetFill += 2f;
        if (classCode == 2f) targetFill -= 2f;

        float targetSmooth = 5f;
        targetSmooth += deathPressure * 2f;
        targetSmooth += magicStyle * 1.2f;
        targetSmooth -= meleeStyle * 0.8f;
        targetSmooth -= perfectRunBonus * 0.5f;
        targetSmooth -= stableRunBonus * 0.2f;

        float enemyMultiplier = 1f;
        enemyMultiplier -= deathPressure * 0.35f;
        enemyMultiplier += perfectRunBonus * 0.12f;
        enemyMultiplier += stableRunBonus * 0.05f;
        enemyMultiplier += successPressure * 0.08f;
        enemyMultiplier += Mathf.Max(0f, coinLoot - 3f) * 0.02f;

        float lootMultiplier = 1f;
        lootMultiplier += deathPressure * 0.45f;
        lootMultiplier += lootNeed * 0.20f;
        lootMultiplier -= perfectRunBonus * 0.10f;
        lootMultiplier -= stableRunBonus * 0.04f;

        float rangedEnemyBias = 0.15f;
        rangedEnemyBias += rangedStyle * 0.15f;
        rangedEnemyBias += magicStyle * 0.20f;
        rangedEnemyBias -= deathPressure * 0.08f;

        float tankEnemyBias = 0.12f;
        tankEnemyBias += meleeStyle * 0.22f;
        tankEnemyBias += perfectRunBonus * 0.04f;
        tankEnemyBias += stableRunBonus * 0.02f;
        tankEnemyBias -= deathPressure * 0.08f;

        return new float[]
        {
            Mathf.Clamp(targetFill, 35f, 58f),
            Mathf.Clamp(targetSmooth, 3f, 9f),
            Mathf.Clamp(enemyMultiplier, 0.65f, 1.45f),
            Mathf.Clamp(lootMultiplier, 0.70f, 1.60f),
            Mathf.Clamp(rangedEnemyBias, 0f, 0.45f),
            Mathf.Clamp(tankEnemyBias, 0f, 0.45f)
        };
    }

    private string[] SplitCsvLine(string line)
    {
        return line.Contains(";")
            ? line.Split(';')
            : line.Split(',');
    }

    private float Parse(string s)
    {
        s = s.Trim();

        if (float.TryParse(
                s,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result))
            return result;

        float.TryParse(
            s,
            NumberStyles.Float,
            CultureInfo.CurrentCulture,
            out result);
        return result;
    }
}
