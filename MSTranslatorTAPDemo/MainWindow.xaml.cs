using System;
using System.Windows;
using System.Diagnostics;
using System.Net;
using System.Windows.Controls;
using System.Web;
using System.IO;
using System.Media;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace MSTranslatorTAPDemo
{
    /// <summary>
    /// The goal of this WPF app is to demonstrate code for getting a security token, and translating a word or phrase into another langauge.
    /// The language of the words to be translated are auto detected. The target langauge is selected from a combobox. The text of the translation is displayed and the       translation is heard as speech.
    /// This app uses the current version of the Microsoft Translator APIs. The v4 version of the APIs will be demonstrated in another app.
    /// The app contains a method to get langauge codes from the service and a dictionary to contain language codes and language names. This will be used in coding exercises.
    /// </summary>
    /// 

    public class AdmAccessToken
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string expires_in { get; set; }
        public string scope { get; set; }
    }

    public partial class MainWindow : Window
    {

        string languageCode = "en"; //set english as the default
        string[] friendlyName = {" "}; //Array for passing languages codes to get friendly name
        List<string> speakLanguages; //List of langauges for speech
        static string headerValue; //used for auth in http header
        Dictionary<string, string> languageCodesAndTitles = new Dictionary<string, string>(); //create dictionary to receive the language codes and friendly names

        public MainWindow()
        {
            InitializeComponent();
            AccessToken(); //Get token, it expire after 10 minutes.
            GetLanguagesForTranslate(); //List of languages that can be translated
            GetLanguageNamesMethod(headerValue, friendlyName); //Friendly name of languages that can be translated
            GetLanguagesForSpeakMethod(headerValue); //List of languages that have a synthetic voice for text to speech
            enumLanguages(); //Create the drop down list of langauges
        }

        //*****POPULATE COMBOBOX*****
        private void enumLanguages()
        {
            
            //run a loop to load the combobox from the dictionary
            var count = languageCodesAndTitles.Count;

            for (int i = 0; i < count; i++)
            {
                LanguageComboBox.Items.Add(languageCodesAndTitles.ElementAt(i).Key);

            }
        }

        
        //*****GET AZURE DATA MARKTER (ADM) TOKEN*****
        private void AccessToken()
        {

            string clientID = "Tele2_MSTS2S_app";
            string clientSecret = "/8cYiAz04B5W1ZbhkdHhpE7fBU+5nxvJCikcX1PTHm0=";

            String strTranslatorAccessURI = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
            String strRequestDetails = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", HttpUtility.UrlEncode(clientID), HttpUtility.UrlEncode(clientSecret));

            WebRequest webRequest = WebRequest.Create(strTranslatorAccessURI);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";

            byte[] bytes = Encoding.ASCII.GetBytes(strRequestDetails);
            webRequest.ContentLength = bytes.Length;

            using (Stream outputStream = webRequest.GetRequestStream())
            {
                outputStream.Write(bytes, 0, bytes.Length);
            }

            WebResponse webResponse = webRequest.GetResponse();

            System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AdmAccessToken));

            //Get deserialized object from Stream
            AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(webResponse.GetResponseStream());

            headerValue = "Bearer " + token.access_token; //create the string for the http header
        }

        //*****BUTTON TO START TRANSLATION PROCESS
        private void translateButton_Click(object sender, EventArgs e)
        {
            AccessToken(); //get an access token for each translation because they expire after 10 minutes.

            languageCodesAndTitles.TryGetValue(LanguageComboBox.Text, out languageCode); //get the language code from the dictionary based on the selection in the combobox

            if (languageCode == null)  //in case no language is selected.
            {
                languageCode = "en";

            }

            //*****BEGIN CODE TO MAKE THE CALL TO THE TRANSLATOR SERVICE TO PERFORM A TRANSLATION FROM THE USER TEXT ENTERED INCLUDES A CALL TO A SPEECH METHOD*****

            string txtToTranslate = textToTranslate.Text;

            string uri = string.Format("http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + System.Web.HttpUtility.UrlEncode(txtToTranslate) + "&to={0}", languageCode);
           
            WebRequest translationWebRequest = WebRequest.Create(uri);

            translationWebRequest.Headers.Add("Authorization", headerValue); //header value is the "Bearer plus the token from ADM

            WebResponse response = null;

            response = translationWebRequest.GetResponse();

            Stream stream = response.GetResponseStream();

            Encoding encode = Encoding.GetEncoding("utf-8");

            StreamReader translatedStream = new StreamReader(stream, encode);

            System.Xml.XmlDocument xTranslation = new System.Xml.XmlDocument();

            xTranslation.LoadXml(translatedStream.ReadToEnd());

            translatedTextLabel.Content = "Translation -->   " + xTranslation.InnerText;

            if (speakLanguages.Contains(languageCode) && txtToTranslate != "")
            {
                //call the method to speak the translated text
                SpeakMethod(headerValue, xTranslation.InnerText, languageCode);
            }
        }

        //*****SPEECH CODE*****
        private void SpeakMethod(string authToken, string textToVoice, String languageCode)
        {
            string translatedString = textToVoice;
            
            string uri = string.Format("http://api.microsofttranslator.com/v2/Http.svc/Speak?text={0}&language={1}&format=" + HttpUtility.UrlEncode("audio/wav") + "&options=MaxQuality", translatedString, languageCode);

            WebRequest webRequest = WebRequest.Create(uri);
            webRequest.Headers.Add("Authorization", authToken);
            WebResponse response = null;
            try
            {
                response = webRequest.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    using (SoundPlayer player = new SoundPlayer(stream))
                    {
                        player.PlaySync();
                    }
                }
            }
            catch
            {

                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }


        //*****CODE TO GET TRANSLATABLE LANGAUGE CODES*****
        private void GetLanguagesForTranslate()
        {
           
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguagesForTranslate";
            WebRequest WebRequest = WebRequest.Create(uri);
            WebRequest.Headers.Add("Authorization", headerValue);

            WebResponse response = null;

            try
            {
                response = WebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {

                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(List<string>));
                    List<string> languagesForTranslate = (List<string>)dcs.ReadObject(stream);
                    friendlyName = languagesForTranslate.ToArray(); //put the list of language codes into an array to pass to the method to get the friendly name.
                    
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }


        //*****CODE TO GET TRANSLATABLE LANGAUGE FRIENDLY NAMES FROM THE TWO CHARACTER CODES*****
        private void GetLanguageNamesMethod(string authToken, string[] languageCodes)
        {
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguageNames?locale=en";
            // create the request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers.Add("Authorization", headerValue);
            request.ContentType = "text/xml";
            request.Method = "POST";
            System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String[]"));
            using (System.IO.Stream stream = request.GetRequestStream())
            {
                dcs.WriteObject(stream, languageCodes);
            }
            WebResponse response = null;
            try
            {
                response = request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    string[] languageNames = (string[])dcs.ReadObject(stream);

                    for (int i = 0; i < languageNames.Length; i++)
                    {

                        languageCodesAndTitles.Add(languageNames[i], languageCodes[i]); //load the dictionary for the combo box

                    }   
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }

        private void GetLanguagesForSpeakMethod(string authToken)
        {

            string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguagesForSpeak";
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add("Authorization", authToken);
            WebResponse response = null;
            try
            {
                response = httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {

                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(List<string>));
                    speakLanguages = (List<string>)dcs.ReadObject(stream);

                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }
    }
}
