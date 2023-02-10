using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace pogodaT
{
    class Program
    {
        static async Task Main(string[] args)
        {
            HttpClient client = new HttpClient();
            int offset = 0;
            string ResponseToUSer = "";
            string key = "c3c398126843eb80375dd3c0dc29408f";
            string token = "6065106008:AAHRvScT6r6cp6Wr4Mb4Cal0aBpj5fxtlUA";
            while (true)
            {
                var responseT = await client.GetAsync("https://api.telegram.org/bot" + token + "/getUpdates?offset=" + offset + "&limit=1");
                var jsonT = await responseT.Content.ReadAsStringAsync();
                Dictionary<string, string> openWithT = JsonParser(jsonT);
                if (responseT.IsSuccessStatusCode && openWithT.Count > 2)
                {
                    offset = int.Parse(openWithT["result.update_id"]) + 1;
                    if (openWithT["result.message.text"] == "/start")
                    {
                        ResponseToUSer = "Введите название города";
                        await client.GetAsync("https://api.telegram.org/bot" + token + "/sendMessage?chat_id=" + openWithT["result.message.chat.id"] + "&text=" + ResponseToUSer);
                    }
                    else
                    {
                        HttpResponseMessage response = await client.GetAsync("https://api.openweathermap.org/data/2.5/weather?q=" + openWithT["result.message.text"] + "&appid=" + key + "&lang=ru&units=metric");
                        if (response.IsSuccessStatusCode)
                        {
                            Dictionary<string, string> dict = JsonParser(await response.Content.ReadAsStringAsync());
                            ResponseToUSer = "Прогноз погоды на " + DateTime.Now + " для города " + dict["name"] + ":\n";
                            ResponseToUSer += "Текущая температура " + dict["main.temp"] + "°, " + dict["weather.description"] + ", ощущается как " + dict["main.feels_like"] + "°\n";
                            ResponseToUSer += "Скорость ветра " + dict["wind.speed"] + " м/с, " + WindDeg(int.Parse(dict["wind.deg"])) + ", влажность " + dict["main.humidity"] + "%, давление " + dict["main.pressure"] + " мм рт. ст.";
                            await client.GetAsync("https://api.telegram.org/bot" + token + "/sendMessage?chat_id=" + openWithT["result.message.chat.id"] + "&text=" + ResponseToUSer);
                        }
                        else
                        {
                            ResponseToUSer = "Неправильный город, или вы не поняли суть бота";
                            await client.GetAsync("https://api.telegram.org/bot" + token + "/sendMessage?chat_id=" + openWithT["result.message.chat.id"] + "&text=" + ResponseToUSer);
                        }
                    }
                }
            }
        }
        static string WindDeg(int deg)
        {
            if (deg > 345 || deg <= 15)
                return "С";
            else if (deg > 15 && deg <= 75)
                return "СВ";
            else if (deg > 75 && deg <= 105)
                return "В";
            else if (deg > 105 && deg <= 165)
                return "ЮВ";
            else if (deg > 165 && deg <= 195)
                return "Ю";
            else if (deg > 195 && deg <= 255)
                return "ЮЗ";
            else if (deg > 255 && deg <= 285)
                return "З";
            else
                return "СЗ";
        }
        static Dictionary<string, string> JsonParser(string json)
        {
            if (json[0] != '[')
                json = "[\n" + json + "\n]";
            var objects = JArray.Parse(json);
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (JObject obj in objects)
            {
                foreach (KeyValuePair<String, JToken> pair in obj)
                {
                    if (pair.Value.Count() > 0)
                    {
                        Dictionary<string, string> miniopenWith = JsonParser(pair.Value.ToString());
                        foreach (string s in miniopenWith.Keys)
                        {
                            if (objects.Count > 1)
                                dict.Add(obj.Path + "." + pair.Key + "." + s, miniopenWith[s]);
                            else if (s[0] == '[')
                                dict.Add(pair.Key + s, miniopenWith[s]);
                            else
                                dict.Add(pair.Key + "." + s, miniopenWith[s]);
                        }
                    }
                    else
                    {
                        if (objects.Count > 1)
                            dict.Add(obj.Path + "." + pair.Key, pair.Value.ToString());
                        else
                            dict.Add(pair.Key, pair.Value.ToString());
                    }
                }
            }
            return dict;
        }
    }
}
