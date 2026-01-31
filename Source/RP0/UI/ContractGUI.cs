using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RP0
{
    public class ContractGUI : UIBase
    {
        public const int MinPayload = 400;
        public const int MaxPayload = 10000;

        public static int CommsPayload = MinPayload;
        public static int WeatherPayload = MinPayload;

        public static Func<string, bool> WithdrawContractAction;

        private static readonly string[] _comSatContracts = new[] { "GEORepeatComSats", "TundraRepeatComSats", "MolniyaRepeatComSats" };
        private static readonly string[] _weatherSatContracts = new[] { "GEOWeather" };

        private static string _newspaperTitle = "Space Gazette";
        private static bool _useLastScreenshot = false;

        private RP0Settings _settings;
        
        // Career Log fields
        private string _exportStatus;
        private string _exportStatusWeb;
        private string _serverUrl;
        private string _token;

        protected override void OnStart()
        {
            _settings = HighLogic.CurrentGame.Parameters.CustomParams<RP0Settings>();
            CommsPayload = _settings.CommsPayload;
            WeatherPayload = _settings.WeatherPayload;
            _newspaperTitle = _settings.NewspaperTitle;
            _useLastScreenshot = _settings.UseLastScreenshot;
            
            // Career Log initialization
            if (!string.IsNullOrWhiteSpace(_settings?.CareerLog_URL))
            {
                byte[] bytes = Convert.FromBase64String(_settings.CareerLog_URL);
                _serverUrl = Encoding.UTF8.GetString(bytes);
            }
            _token = _settings?.CareerLog_Token;
        }

        public void RenderContractsTab()
        {
            GUILayout.BeginVertical();
            try
            {
                GUILayout.Label($"Use this tab to set the required payload amount for contracts.", BoldLabel);
                GUILayout.Space(10f);

                GUILayout.Label($"CommSat Payload: {CommsPayload}", HighLogic.Skin.label, GUILayout.Width(250));
                float commsAmnt = GUILayout.HorizontalSlider(CommsPayload, MinPayload, MaxPayload, HighLogic.Skin.horizontalSlider, HighLogic.Skin.horizontalSliderThumb);
                CommsPayload = Mathf.RoundToInt(commsAmnt / 100) * 100;    // slider works in increments of 100
                GUILayout.Space(5f);
                GUILayout.Label($"WeatherSat Payload: {WeatherPayload}", HighLogic.Skin.label, GUILayout.Width(250));
                float weatherAmnt = GUILayout.HorizontalSlider(WeatherPayload, MinPayload, MaxPayload, HighLogic.Skin.horizontalSlider, HighLogic.Skin.horizontalSliderThumb);
                WeatherPayload = Mathf.RoundToInt(weatherAmnt / 100) * 100;

                if (_settings.CommsPayload != CommsPayload)
                {
                    foreach (string contractName in _comSatContracts)
                    {
                        WithdrawContractAction?.Invoke(contractName);
                    }
                }

                if (_settings.WeatherPayload != WeatherPayload)
                {
                    foreach (string contractName in _weatherSatContracts)
                    {
                        WithdrawContractAction?.Invoke(contractName);
                    }
                }

                _settings.CommsPayload = CommsPayload;
                _settings.WeatherPayload = WeatherPayload;
            }
            finally
            {
                GUILayout.EndVertical();
            }

            GUILayout.BeginVertical();
            try
            {
                GUILayout.Space(10f);
                GUILayout.Space(10f);
                GUILayout.Label($"Use this area to change the name of the Newspaper Title.", BoldLabel);
                GUILayout.Space(10f);

                GUILayout.Label($"Newspaper Title: (18 characters max)", HighLogic.Skin.label, GUILayout.Width(250));
                _newspaperTitle = GUILayout.TextField(_newspaperTitle, HighLogic.Skin.textField);

                _settings.NewspaperTitle = _newspaperTitle;
            }
            finally
            {
                GUILayout.EndVertical();
            }

            GUILayout.BeginVertical();
            try
            {
                GUILayout.Space(10f);
                GUILayout.Space(10f);
                GUILayout.Label($"Use this area to set how the automatic newspaper images are created. Not selected will take an automatic screenshot.", BoldLabel);
                GUILayout.Space(10f);
                _useLastScreenshot = GUILayout.Toggle(_useLastScreenshot, "Use last screenshot instead of auto screenshot for Newspaper");
                _settings.UseLastScreenshot = _useLastScreenshot;
            }
            finally
            {
                GUILayout.EndVertical();
            }
            
            // Career Log Export Section
            GUILayout.BeginVertical();
            try
            {
                GUILayout.Space(10f);
                GUILayout.Space(10f);
                GUILayout.Label($"Use this area to export your career progress.", BoldLabel);
                GUILayout.Space(10f);
                
                if (GUILayout.Button("Export to file", HighLogic.Skin.button, GUILayout.ExpandWidth(false), GUILayout.Height(30), GUILayout.Width(125)))
                {
                    try
                    {
                        string path = KSPUtil.ApplicationRootPath + "/RP-1_Career.csv";
                        CareerLog.Instance.ExportToFile(path);
                        _exportStatus = $"Career progress exported to {Path.GetFullPath(path)}";
                    }
                    catch (Exception ex)
                    {
                        _exportStatus = $"Export failed: {ex.Message}";
                    }
                }
                GUILayout.Label(_exportStatus);

                GUILayout.Space(10f);
                GUILayout.Label("Server URL:");
                _serverUrl = GUILayout.TextField(_serverUrl, HighLogic.Skin.textField);

                GUILayout.Label("Token:");
                _token = GUILayout.TextField(_token, HighLogic.Skin.textField);

                if (GUILayout.Button("Export to web", HighLogic.Skin.button, GUILayout.ExpandWidth(false), GUILayout.Height(30),
                    GUILayout.Width(125)))
                {
                    try
                    {
                        _settings.CareerLog_URL = Convert.ToBase64String(Encoding.UTF8.GetBytes(_serverUrl));    // KSP really doesn't like the symbols that a typical URL contains
                        _settings.CareerLog_Token = _token;

                        _exportStatusWeb = null;
                        CareerLog.Instance.ExportToWeb(_serverUrl, _token, () =>
                        {
                            _exportStatusWeb = "Career progress exported to web.";
                        }, (errorMsg) =>
                        {
                            _exportStatusWeb = $"Career progress export failed. {errorMsg}";
                        });
                    }
                    catch (Exception ex)
                    {
                        RP0Debug.LogError($"{ex}");
                        _exportStatusWeb = $"Export failed: {ex.Message}";
                    }
                }
                GUILayout.Label(_exportStatusWeb);
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }
    }
}
