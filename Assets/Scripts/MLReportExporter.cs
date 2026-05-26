using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class MLReportExporter
{
    private const int ChartWidth = 760;
    private const int ChartHeight = 260;
    private const int Padding = 36;

    public static string Export(string folder)
    {
        if (string.IsNullOrEmpty(folder))
            return "";

        Directory.CreateDirectory(folder);

        string predictionsPath = Path.Combine(folder, "ml_predictions.csv");
        string runsPath = Path.Combine(folder, "player_data.csv");
        string mapMetricsPath =
            Path.Combine(folder, "adaptive_map_metrics.csv");
        string reportPath = Path.Combine(folder, "ml_report.html");

        UpgradePredictionHeader(predictionsPath);

        List<float> mae = ReadColumn(predictionsPath, "mae");
        List<float> mse = ReadColumn(predictionsPath, "mse");
        List<float> r2 = ReadColumn(predictionsPath, "r2");
        List<float> performance = ReadColumn(runsPath, "performanceScore");
        List<float> appliedEnemy =
            ReadColumn(predictionsPath, "appliedEnemyMultiplier");
        List<float> appliedLoot =
            ReadColumn(predictionsPath, "appliedLootMultiplier");
        List<float> baseFill = ReadColumn(predictionsPath, "baseFill");
        List<float> appliedFill = ReadColumn(predictionsPath, "appliedFill");
        List<float> baseSmooth = ReadColumn(predictionsPath, "baseSmooth");
        List<float> appliedSmooth = ReadColumn(predictionsPath, "appliedSmooth");
        List<float> baseWidth = ReadColumn(predictionsPath, "baseWidth");
        List<float> baseHeight = ReadColumn(predictionsPath, "baseHeight");
        List<float> appliedWidth = ReadColumn(predictionsPath, "appliedWidth");
        List<float> appliedHeight = ReadColumn(predictionsPath, "appliedHeight");
        List<float> mapWalkable = ReadColumn(mapMetricsPath, "walkable");
        List<float> mapEntropy = ReadColumn(mapMetricsPath, "entropy");
        Dictionary<string, string> latestPrediction =
            ReadLatestRow(predictionsPath);
        Dictionary<string, string> latestMapMetrics =
            ReadLatestRow(mapMetricsPath);

        StringBuilder html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"uk\"><head><meta charset=\"utf-8\">");
        html.AppendLine("<title>Звіт перевірки ML</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:32px;background:#f7f8fa;color:#17202a}");
        html.AppendLine("section{background:white;border:1px solid #dde2e8;border-radius:8px;padding:20px;margin:0 0 18px}");
        html.AppendLine("h1,h2{margin:0 0 14px}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}");
        html.AppendLine(".metric{background:#eef3f8;border-radius:6px;padding:12px}.metric b{display:block;font-size:24px}");
        html.AppendLine(".badge{display:inline-block;background:#17202a;color:white;border-radius:999px;padding:8px 12px;margin:0 8px 8px 0;font-weight:600}");
        html.AppendLine(".comparison{display:grid;grid-template-columns:1fr 1fr 1fr;gap:12px}.box{border:1px solid #dde2e8;border-radius:6px;padding:14px}.delta{font-size:28px;font-weight:700}");
        html.AppendLine("table{width:100%;border-collapse:collapse}td,th{border-bottom:1px solid #dde2e8;text-align:left;padding:9px}");
        html.AppendLine("code{background:#eef3f8;padding:2px 5px;border-radius:4px}.formula{font-size:18px;line-height:1.7}");
        html.AppendLine("svg{width:100%;max-width:900px;height:auto;background:#fbfcfe;border:1px solid #dde2e8}");
        html.AppendLine(".chart-note{color:#475569;margin:0 0 12px}.chart-summary{display:flex;gap:10px;flex-wrap:wrap;margin:10px 0 0}.pill{background:#eef3f8;border-radius:999px;padding:6px 10px}");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<h1>Звіт перевірки ML</h1>");
        html.AppendLine($"<p>Згенеровано: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

        AppendActiveSetup(html, latestPrediction, latestMapMetrics);
        AppendExplanation(html);

        html.AppendLine("<section><h2>Набір даних</h2><div class=\"grid\">");
        AppendMetric(html, "Завершені забіги", CountDataRows(runsPath).ToString());
        AppendMetric(html, "Прогнози", CountDataRows(predictionsPath).ToString());
        AppendMetric(html, "Папка звіту", Escape(folder));
        html.AppendLine("</div></section>");

        html.AppendLine("<section><h2>Формули перевірки</h2>");
        html.AppendLine("<div class=\"formula\">");
        html.AppendLine("<div><code>prediction y_j = b_j + sum(x_i * w_ij)</code></div>");
        html.AppendLine("<div><code>MAE = (1/n) * sum(|y - y_hat|)</code></div>");
        html.AppendLine("<div><code>MSE = (1/n) * sum((y - y_hat)^2)</code></div>");
        html.AppendLine("<div><code>R2 = 1 - SS_res / SS_tot</code></div>");
        html.AppendLine("</div></section>");

        html.AppendLine("<section><h2>Останні метрики</h2><div class=\"grid\">");
        AppendMetric(html, "MAE", LastOrDash(mae, "F4"));
        AppendMetric(html, "MSE", LastOrDash(mse, "F4"));
        AppendMetric(html, "R2", LastOrDash(r2, "F4"));
        html.AppendLine("</div></section>");

        AppendComparison(
            html,
            baseFill,
            appliedFill,
            baseSmooth,
            appliedSmooth,
            baseWidth,
            baseHeight,
            appliedWidth,
            appliedHeight,
            appliedEnemy,
            appliedLoot);
        AppendAutomaticConclusion(
            html,
            mae,
            mse,
            r2,
            baseFill,
            appliedFill,
            baseSmooth,
            appliedSmooth,
            appliedEnemy,
            appliedLoot,
            performance);
        AppendMapMetrics(html, latestMapMetrics);

        html.AppendLine("<section><h2>Основні графіки</h2>");
        html.AppendLine("<p class=\"chart-note\">У звіті залишені тільки ключові графіки: якість прогнозу, результативність гравця та два параметри адаптації складності.</p>");
        html.AppendLine("</section>");

        AppendChartSection(html, "MAE за прогнозами", mae, "#1f77b4",
            "X - номер прогнозу, Y - середня абсолютна похибка. Чим нижче значення, тим точніші прогнози моделі.");
        AppendChartSection(html, "R2 за прогнозами", r2, "#2ca02c",
            "X - номер прогнозу, Y - коефіцієнт детермінації. Ближче до 1 означає кращу відповідність моделі даним.");
        AppendChartSection(html, "Оцінка результативності за забігами", performance, "#9467bd",
            "X - номер забігу, Y - інтегральна оцінка дій гравця: вбивства, лут, смерті та проходження.");
        AppendChartSection(html, "Застосований множник ворогів", appliedEnemy, "#ff7f0e",
            "X - номер прогнозу, Y - множник кількості ворогів. Значення нижче 1 зменшує тиск, вище 1 підвищує складність.");
        AppendChartSection(html, "Застосований множник луту", appliedLoot, "#17becf",
            "X - номер прогнозу, Y - множник кількості луту. Значення вище 1 означає більше ресурсів для гравця.");

        html.AppendLine("</body></html>");
        string reportHtml = html.ToString();
        File.WriteAllText(reportPath, reportHtml, Encoding.UTF8);

        string projectReportPath = TryWriteProjectCopy(reportHtml);
        string visibleReportPath = string.IsNullOrEmpty(projectReportPath)
            ? reportPath
            : projectReportPath;

        Debug.Log("ML report saved: " + visibleReportPath);
        return visibleReportPath;
    }

    public static void AppendDemoData(string folder)
    {
        Directory.CreateDirectory(folder);

        string runsPath = Path.Combine(folder, "player_data.csv");
        string predictionsPath = Path.Combine(folder, "ml_predictions.csv");

        if (!File.Exists(runsPath))
        {
            File.WriteAllText(
                runsPath,
                "run;result;performanceScore;class;meleeAbW;rangedAbW;" +
                "magicAbW;meleeRate;rangedRate;" +
                "magicRate;deaths;hpLoot;" +
                "manaLoot;coinLoot\n");
        }

        int firstRun = CountDataRows(runsPath) + 1;
        string[] runResults = { "Death", "Complete", "Death", "Complete", "Complete", "Death" };
        float[] scores = { 3.5f, 10.2f, 5.8f, 14.6f, 17.4f, 8.1f };

        StringBuilder runLines = new StringBuilder();
        for (int i = 0; i < runResults.Length; i++)
        {
            int run = firstRun + i;
            int deaths = runResults[i] == "Complete" ? 0 : 1;
            runLines.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0};{1};{2:F2};{3};{4:F2};{5:F2};{6:F2};{7:F2};{8:F2};{9:F2};{10};{11};{12};{13}\n",
                run,
                runResults[i],
                scores[i],
                1 + i % 3,
                1.2f - i * 0.05f,
                0.4f + i * 0.08f,
                0.6f + i * 0.04f,
                Mathf.Clamp01(0.55f - i * 0.04f),
                Mathf.Clamp01(0.25f + i * 0.03f),
                Mathf.Clamp01(0.20f + i * 0.01f),
                deaths,
                1 + i % 3,
                i % 2,
                2 + i);
        }
        File.AppendAllText(runsPath, runLines.ToString());

        if (!File.Exists(predictionsPath))
            File.WriteAllText(predictionsPath, PredictionHeader());

        int firstPrediction = CountDataRows(predictionsPath) + 1;
        StringBuilder predictionLines = new StringBuilder();
        for (int i = 0; i < 6; i++)
        {
            float mae = 1.2f / (i + 1);
            float mse = 2.4f / (i + 1);
            float r2 = Mathf.Clamp01(0.35f + i * 0.10f);
            int baseFill = 45;
            int appliedFill = Mathf.RoundToInt(46 + i * 0.8f);
            int baseSmooth = 5;
            int appliedSmooth = Mathf.Clamp(5 + i % 3 - 1, 3, 9);

            predictionLines.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0};{1};Demo;Easy;1;0;0;0.3;0.2;0.1;0.5;0.3;0.2;0;1;0;3;1;{2:F2};{3:F2};{4:F2};{5:F3};{6:F3};{7:F3};{8:F3};{9};{10};{11:F3};{12:F3};{13:F3};{14:F3};{15:F4};{16:F4};{17:F4};{18};{19};{20};{21};{22};{23}\n",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                firstPrediction + i,
                scores[Mathf.Min(i, scores.Length - 1)],
                appliedFill,
                appliedSmooth,
                0.95f + i * 0.04f,
                1.15f - i * 0.03f,
                0.12f + i * 0.02f,
                0.10f + i * 0.02f,
                appliedFill,
                appliedSmooth,
                0.95f + i * 0.04f,
                1.15f - i * 0.03f,
                0.12f + i * 0.02f,
                0.10f + i * 0.02f,
                mae,
                mse,
                r2,
                baseFill,
                baseSmooth,
                60,
                60,
                60,
                60);
        }
        File.AppendAllText(predictionsPath, predictionLines.ToString());
    }

    public static string PredictionHeader()
    {
        return "time;run;class;difficulty;" +
               "isWarrior;isMage;isRogue;" +
               "meleeAbW;rangedAbW;magicAbW;" +
               "meleeRate;rangedRate;magicRate;" +
               "deaths;hpLoot;manaLoot;coinLoot;" +
               "runCompleted;performanceScore;" +
               "predFill;predSmooth;predEnemyMultiplier;" +
               "predLootMultiplier;predRangedBias;predTankBias;" +
               "appliedFill;appliedSmooth;appliedEnemyMultiplier;" +
               "appliedLootMultiplier;appliedRangedBias;appliedTankBias;" +
               "mae;mse;r2;baseFill;baseSmooth;" +
               "baseWidth;baseHeight;appliedWidth;appliedHeight\n";
    }

    private static void AppendActiveSetup(
        StringBuilder html,
        Dictionary<string, string> prediction,
        Dictionary<string, string> mapMetrics)
    {
        string difficulty = GetValue(prediction, "difficulty", "-");
        string mapMethod = GetValue(mapMetrics, "mapMethod", "-");
        string className = GetValue(prediction, "class", "-");

        html.AppendLine("<section><h2>Активні налаштування</h2>");
        html.AppendLine($"<span class=\"badge\">Складність: {Escape(difficulty)}</span>");
        html.AppendLine($"<span class=\"badge\">Метод карти: {Escape(mapMethod)}</span>");
        html.AppendLine($"<span class=\"badge\">Клас: {Escape(className)}</span>");
        html.AppendLine("</section>");
    }

    private static void AppendExplanation(StringBuilder html)
    {
        html.AppendLine("<section><h2>Як використовується ML</h2>");
        html.AppendLine("<p>Модель не створює карту напряму. Вона читає статистику гравця з CSV, навчає багатовихідну лінійну регресію та прогнозує параметри для процедурного генератора.</p>");
        html.AppendLine("<table>");
        html.AppendLine("<tr><th>Приклади входів</th><td>клас, ваги вибраних здібностей, стиль бою, смерті, підібраний лут, факт проходження, оцінка результативності</td></tr>");
        html.AppendLine("<tr><th>Прогнозовані виходи</th><td>відсоток заповнення карти, кількість ітерацій згладжування, множник ворогів, множник луту, зміщення дальніх ворогів, зміщення ворогів-танків</td></tr>");
        html.AppendLine("<tr><th>Перевірка</th><td>MAE і MSE оцінюють похибку прогнозу. R2 показує, яку частину зміни цільових значень пояснює модель.</td></tr>");
        html.AppendLine("</table></section>");
    }

    private static void AppendComparison(
        StringBuilder html,
        List<float> baseFill,
        List<float> appliedFill,
        List<float> baseSmooth,
        List<float> appliedSmooth,
        List<float> baseWidth,
        List<float> baseHeight,
        List<float> appliedWidth,
        List<float> appliedHeight,
        List<float> appliedEnemy,
        List<float> appliedLoot)
    {
        html.AppendLine("<section><h2>Порівняння карти без ML та з ML</h2>");
        html.AppendLine("<p class=\"chart-note\">Колонка \"Без ML\" показує базові параметри генератора до застосування прогнозу. Колонка \"З ML\" показує параметри, які були застосовані після прогнозу моделі.</p>");
        html.AppendLine("<table>");
        html.AppendLine("<tr><th>Параметр</th><th>Без ML</th><th>З ML</th><th>Зміна</th></tr>");
        AppendBeforeAfterRow(html, "Ширина карти", baseWidth, appliedWidth, "F0");
        AppendBeforeAfterRow(html, "Висота карти", baseHeight, appliedHeight, "F0");
        AppendBeforeAfterRow(html, "Заповнення стінами", baseFill, appliedFill, "F0");
        AppendBeforeAfterRow(html, "Ітерації згладжування", baseSmooth, appliedSmooth, "F0");
        AppendBeforeAfterRow(html, "Множник ворогів", null, appliedEnemy, "F2", "1.00");
        AppendBeforeAfterRow(html, "Множник луту", null, appliedLoot, "F2", "1.00");
        html.AppendLine("</table>");

        html.AppendLine("<h3>Коротко</h3>");
        html.AppendLine("<div class=\"comparison\">");
        AppendComparisonBox(
            html,
            "Заповнення карти",
            LastOrDash(baseFill, "F0"),
            LastOrDash(appliedFill, "F0"),
            DeltaOrDash(baseFill, appliedFill, "F0"));
        AppendComparisonBox(
            html,
            "Ітерації згладжування",
            LastOrDash(baseSmooth, "F0"),
            LastOrDash(appliedSmooth, "F0"),
            DeltaOrDash(baseSmooth, appliedSmooth, "F0"));
        AppendComparisonBox(
            html,
            "Множники контенту",
            "Вороги x" + LastOrDash(appliedEnemy, "F2"),
            "Лут x" + LastOrDash(appliedLoot, "F2"),
            "адаптивно");
        html.AppendLine("</div></section>");
    }

    private static void AppendBeforeAfterRow(
        StringBuilder html,
        string label,
        List<float> beforeValues,
        List<float> afterValues,
        string format,
        string fixedBefore = null)
    {
        string before = fixedBefore ?? LastOrDash(beforeValues, format);
        string after = LastOrDash(afterValues, format);
        string delta = fixedBefore != null
            ? DeltaFromFixedOrDash(float.Parse(fixedBefore, CultureInfo.InvariantCulture), afterValues, format)
            : DeltaOrDash(beforeValues, afterValues, format);

        html.AppendLine(
            $"<tr><th>{Escape(label)}</th><td>{Escape(before)}</td><td>{Escape(after)}</td><td>{Escape(delta)}</td></tr>");
    }

    private static void AppendComparisonBox(
        StringBuilder html,
        string title,
        string before,
        string after,
        string delta)
    {
        html.AppendLine("<div class=\"box\">");
        html.AppendLine($"<h3>{Escape(title)}</h3>");
        html.AppendLine($"<div>Базове: <b>{Escape(before)}</b></div>");
        html.AppendLine($"<div>Застосоване: <b>{Escape(after)}</b></div>");
        html.AppendLine($"<div class=\"delta\">{Escape(delta)}</div>");
        html.AppendLine("</div>");
    }

    private static void AppendAutomaticConclusion(
        StringBuilder html,
        List<float> mae,
        List<float> mse,
        List<float> r2,
        List<float> baseFill,
        List<float> appliedFill,
        List<float> baseSmooth,
        List<float> appliedSmooth,
        List<float> appliedEnemy,
        List<float> appliedLoot,
        List<float> performance)
    {
        html.AppendLine("<section><h2>Автоматичний висновок</h2>");
        html.AppendLine("<ul>");

        if (mae.Count >= 2)
            html.AppendLine("<li>" + Escape(BuildTrendText("MAE", mae, true)) + "</li>");
        if (mse.Count >= 2)
            html.AppendLine("<li>" + Escape(BuildTrendText("MSE", mse, true)) + "</li>");
        if (r2.Count >= 2)
            html.AppendLine("<li>" + Escape(BuildTrendText("R2", r2, false)) + "</li>");
        if (performance.Count > 0)
            html.AppendLine(
                $"<li>Остання оцінка результативності: {LastOrDash(performance, "F2")}.</li>");

        html.AppendLine(
            "<li>" +
            Escape(BuildAdaptationText(
                baseFill,
                appliedFill,
                baseSmooth,
                appliedSmooth,
                appliedEnemy,
                appliedLoot)) +
            "</li>");

        html.AppendLine("</ul></section>");
    }

    private static void AppendMapMetrics(
        StringBuilder html,
        Dictionary<string, string> metrics)
    {
        html.AppendLine("<section><h2>Останні метрики карти</h2>");
        if (metrics.Count == 0)
        {
            html.AppendLine("<p>Метрик карти ще немає.</p></section>");
            return;
        }

        html.AppendLine("<table>");
        AppendTableRow(html, "Час", GetValue(metrics, "time", "-"));
        AppendTableRow(html, "Білд", GetValue(metrics, "build", "-"));
        AppendTableRow(html, "Метод карти", GetValue(metrics, "mapMethod", "-"));
        AppendTableRow(
            html,
            "Розмір",
            GetValue(metrics, "width", "-") + " x " +
            GetValue(metrics, "height", "-"));
        AppendTableRow(html, "Заповнення", GetValue(metrics, "fill", "-"));
        AppendTableRow(html, "Згладжування", GetValue(metrics, "smooth", "-"));
        AppendTableRow(
            html,
            "Прохідна площа",
            GetValue(metrics, "walkable", "-") + "%");
        AppendTableRow(html, "Ентропія", GetValue(metrics, "entropy", "-"));
        AppendTableRow(
            html,
            "Множник ворогів",
            GetValue(metrics, "enemyMultiplier", "-"));
        AppendTableRow(
            html,
            "Множник луту",
            GetValue(metrics, "lootMultiplier", "-"));
        AppendTableRow(
            html,
            "Зміщення типів ворогів",
            "дальній " + GetValue(metrics, "rangedBias", "-") +
            ", танк " + GetValue(metrics, "tankBias", "-"));
        html.AppendLine("</table></section>");
    }

    private static void AppendTableRow(
        StringBuilder html,
        string name,
        string value)
    {
        html.AppendLine(
            $"<tr><th>{Escape(name)}</th><td>{Escape(value)}</td></tr>");
    }

    private static void AppendMetric(
        StringBuilder html,
        string label,
        string value)
    {
        html.AppendLine(
            $"<div class=\"metric\"><span>{Escape(label)}</span><b>{Escape(value)}</b></div>");
    }

    private static string TryWriteProjectCopy(string reportHtml)
    {
        try
        {
            DirectoryInfo assetsDirectory =
                Directory.GetParent(Application.dataPath);
            if (assetsDirectory == null)
                return "";

            string path = Path.Combine(
                assetsDirectory.FullName,
                "ML_Report.html");
            File.WriteAllText(path, reportHtml, Encoding.UTF8);
            return path;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "Could not write project ML report copy: " +
                ex.Message);
            return "";
        }
    }

    private static void AppendChartSection(
        StringBuilder html,
        string title,
        List<float> values,
        string color,
        string description)
    {
        html.AppendLine($"<section><h2>{Escape(title)}</h2>");
        html.AppendLine($"<p class=\"chart-note\">{Escape(description)}</p>");
        html.AppendLine(BuildLineChart(values, color, "Номер запису", "Значення"));
        AppendChartSummary(html, values);
        html.AppendLine("</section>");
    }

    private static string BuildLineChart(
        List<float> values,
        string color,
        string xLabel,
        string yLabel)
    {
        if (values.Count == 0)
            return "<p>Даних ще немає.</p>";

        int left = 70;
        int right = 28;
        int top = 28;
        int bottom = 56;
        int plotWidth = ChartWidth - left - right;
        int plotHeight = ChartHeight - top - bottom;
        float min = values[0];
        float max = values[0];
        foreach (float value in values)
        {
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }

        if (Mathf.Approximately(min, max))
        {
            min -= 1f;
            max += 1f;
        }

        StringBuilder polyline = new StringBuilder();
        StringBuilder circles = new StringBuilder();
        StringBuilder grid = new StringBuilder();
        StringBuilder yLabels = new StringBuilder();

        for (int i = 0; i <= 4; i++)
        {
            float t = i / 4f;
            float y = top + plotHeight - t * plotHeight;
            float value = Mathf.Lerp(min, max, t);

            grid.AppendFormat(
                CultureInfo.InvariantCulture,
                "<line x1=\"{0}\" y1=\"{1:F1}\" x2=\"{2}\" y2=\"{1:F1}\" stroke=\"#e3e8ef\"/>",
                left,
                y,
                left + plotWidth);
            yLabels.AppendFormat(
                CultureInfo.InvariantCulture,
                "<text x=\"8\" y=\"{0:F1}\" font-size=\"12\" fill=\"#526070\">{1:F2}</text>",
                y + 4f,
                value);
        }

        int xTickCount = Mathf.Min(values.Count, 6);
        StringBuilder xLabels = new StringBuilder();
        for (int i = 0; i < xTickCount; i++)
        {
            float t = xTickCount == 1 ? 0f : (float)i / (xTickCount - 1);
            int index = Mathf.RoundToInt(t * (values.Count - 1));
            float x = left + t * plotWidth;
            xLabels.AppendFormat(
                CultureInfo.InvariantCulture,
                "<text x=\"{0:F1}\" y=\"{1}\" font-size=\"12\" fill=\"#526070\" text-anchor=\"middle\">{2}</text>",
                x,
                ChartHeight - 34,
                index + 1);
        }

        for (int i = 0; i < values.Count; i++)
        {
            float t = values.Count == 1
                ? 0f
                : (float)i / (values.Count - 1);
            float x = left + t * plotWidth;
            float normalized = Mathf.InverseLerp(min, max, values[i]);
            float y = top + plotHeight - normalized * plotHeight;

            if (polyline.Length > 0) polyline.Append(" ");
            polyline.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:F1},{1:F1}",
                x,
                y);
            circles.AppendFormat(
                CultureInfo.InvariantCulture,
                "<circle cx=\"{0:F1}\" cy=\"{1:F1}\" r=\"4\" fill=\"white\" stroke=\"{2}\" stroke-width=\"2\"><title>Запис {3}: {4:F4}</title></circle>",
                x,
                y,
                color,
                i + 1,
                values[i]);
        }

        string lastLabel = "";
        if (values.Count > 0)
        {
            float lastT = values.Count == 1
                ? 0f
                : 1f;
            float lastX = left + lastT * plotWidth;
            float lastNorm = Mathf.InverseLerp(min, max, values[values.Count - 1]);
            float lastY = top + plotHeight - lastNorm * plotHeight;
            lastLabel = string.Format(
                CultureInfo.InvariantCulture,
                "<text x=\"{0:F1}\" y=\"{1:F1}\" font-size=\"13\" fill=\"{2}\" font-weight=\"700\">останнє: {3:F3}</text>",
                Mathf.Max(left + 8f, lastX - 120f),
                Mathf.Max(top + 16f, lastY - 10f),
                color,
                values[values.Count - 1]);
        }

        return
            $"<svg viewBox=\"0 0 {ChartWidth} {ChartHeight}\" role=\"img\">" +
            grid +
            $"<line x1=\"{left}\" y1=\"{top + plotHeight}\" x2=\"{left + plotWidth}\" y2=\"{top + plotHeight}\" stroke=\"#8b98a7\"/>" +
            $"<line x1=\"{left}\" y1=\"{top}\" x2=\"{left}\" y2=\"{top + plotHeight}\" stroke=\"#8b98a7\"/>" +
            yLabels +
            xLabels +
            $"<text x=\"{left + plotWidth / 2}\" y=\"{ChartHeight - 10}\" font-size=\"13\" fill=\"#334155\" text-anchor=\"middle\">{Escape(xLabel)}</text>" +
            $"<text x=\"18\" y=\"18\" font-size=\"13\" fill=\"#334155\">{Escape(yLabel)}</text>" +
            $"<polyline fill=\"none\" stroke=\"{color}\" stroke-width=\"3\" points=\"{polyline}\"/>" +
            circles +
            lastLabel +
            "</svg>";
    }

    private static void AppendChartSummary(
        StringBuilder html,
        List<float> values)
    {
        if (values.Count == 0)
            return;

        float min = values[0];
        float max = values[0];
        foreach (float value in values)
        {
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }

        html.AppendLine("<div class=\"chart-summary\">");
        html.AppendLine($"<span class=\"pill\">Записів: {values.Count}</span>");
        html.AppendLine($"<span class=\"pill\">Останнє: {values[values.Count - 1]:F4}</span>");
        html.AppendLine($"<span class=\"pill\">Мін: {min:F4}</span>");
        html.AppendLine($"<span class=\"pill\">Макс: {max:F4}</span>");
        html.AppendLine("</div>");
    }

    private static List<float> ReadColumn(string path, string columnName)
    {
        List<float> values = new List<float>();
        if (!File.Exists(path)) return values;

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2) return values;

        string[] headers = SplitLine(lines[0]);
        int column = Array.IndexOf(headers, columnName);
        if (column < 0) return values;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = SplitLine(lines[i]);
            if (parts.Length <= column) continue;
            if (TryParse(parts[column], out float value))
                values.Add(value);
        }

        return values;
    }

    private static Dictionary<string, string> ReadLatestRow(string path)
    {
        Dictionary<string, string> row = new Dictionary<string, string>();
        if (!File.Exists(path)) return row;

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2) return row;

        string[] headers = SplitLine(lines[0]);
        string[] values = SplitLine(lines[lines.Length - 1]);

        for (int i = 0; i < headers.Length && i < values.Length; i++)
            row[headers[i]] = values[i].Trim().Trim('"');

        return row;
    }

    private static void UpgradePredictionHeader(string path)
    {
        if (!File.Exists(path)) return;

        string[] lines = File.ReadAllLines(path);
        if (lines.Length == 0) return;
        if (lines[0].Contains("baseFill") &&
            lines[0].Contains("baseSmooth") &&
            lines[0].Contains("appliedHeight"))
            return;

        lines[0] = PredictionHeader().TrimEnd();
        File.WriteAllLines(path, lines);
    }

    private static int CountDataRows(string path)
    {
        if (!File.Exists(path)) return 0;
        return Mathf.Max(0, File.ReadAllLines(path).Length - 1);
    }

    private static string LastOrDash(List<float> values, string format)
    {
        if (values.Count == 0) return "-";
        return values[values.Count - 1].ToString(
            format,
            CultureInfo.InvariantCulture);
    }

    private static string DeltaOrDash(
        List<float> before,
        List<float> after,
        string format)
    {
        if (before.Count == 0 || after.Count == 0)
            return "-";

        float delta = after[after.Count - 1] - before[before.Count - 1];
        string sign = delta > 0f ? "+" : "";
        return sign + delta.ToString(format, CultureInfo.InvariantCulture);
    }

    private static string DeltaFromFixedOrDash(
        float before,
        List<float> after,
        string format)
    {
        if (after == null || after.Count == 0)
            return "-";

        float delta = after[after.Count - 1] - before;
        string sign = delta > 0f ? "+" : "";
        return sign + delta.ToString(format, CultureInfo.InvariantCulture);
    }

    private static string BuildTrendText(
        string metric,
        List<float> values,
        bool lowerIsBetter)
    {
        float first = values[0];
        float last = values[values.Count - 1];
        bool improved = lowerIsBetter ? last < first : last > first;
        string direction = last > first ? "зросла" : "зменшилась";
        string meaning = improved
            ? "це позитивна динаміка"
            : "потрібно більше даних або стабільніше навчання";

        return $"{metric} {direction} з {first:F4} до {last:F4}; {meaning}.";
    }

    private static string BuildAdaptationText(
        List<float> baseFill,
        List<float> appliedFill,
        List<float> baseSmooth,
        List<float> appliedSmooth,
        List<float> appliedEnemy,
        List<float> appliedLoot)
    {
        if (appliedEnemy.Count == 0 || appliedLoot.Count == 0)
            return "Поки недостатньо прогнозів, щоб пояснити адаптацію.";

        string fillChange = DeltaOrDash(baseFill, appliedFill, "F0");
        string smoothChange = DeltaOrDash(baseSmooth, appliedSmooth, "F0");
        float enemy = appliedEnemy[appliedEnemy.Count - 1];
        float loot = appliedLoot[appliedLoot.Count - 1];

        string enemyText = enemy < 1f
            ? "зменшила тиск ворогів"
            : "збільшила тиск ворогів";
        string lootText = loot > 1f
            ? "збільшила підтримку через лут"
            : "зменшила додаткову підтримку лутом";

        return "Остання адаптація змінила fill на " + fillChange +
               ", smooth на " + smoothChange + ", " +
               enemyText + " і " + lootText + ".";
    }

    private static string GetValue(
        Dictionary<string, string> row,
        string key,
        string fallback)
    {
        return row.TryGetValue(key, out string value) &&
               !string.IsNullOrEmpty(value)
            ? value
            : fallback;
    }

    private static string[] SplitLine(string line)
    {
        return line.Contains(";")
            ? line.Split(';')
            : line.Split(',');
    }

    private static bool TryParse(string text, out float value)
    {
        text = text.Trim().Trim('"');
        if (float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value))
            return true;

        return float.TryParse(
            text,
            NumberStyles.Float,
            CultureInfo.CurrentCulture,
            out value);
    }

    private static string Escape(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
