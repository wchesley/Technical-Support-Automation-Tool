﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Chrome;
using System.Threading;
using OpenQA.Selenium; 

namespace LMGD_Tester
{
    class Radio
    {
        public static void GetRadioIP(ChromeDriver browser, string customerNumber)
        {
            var custNumber = browser.FindElementByName("customer_number");
            var RadioForm = browser.FindElementsByName("B1");
            //var RadioForm = browser.FindElementByXPath(@"//*[@id='div_3_contents']/form");
            custNumber.SendKeys(customerNumber);
            RadioForm[2].Submit();
            //Console.ReadKey();
            OpenQA.Selenium.Support.UI.WebDriverWait wait = new OpenQA.Selenium.Support.UI.WebDriverWait(browser, System.TimeSpan.FromSeconds(10));
            Thread.Sleep(150);
            Console.WriteLine(browser.Url);
            Console.ReadKey();


            //await table to load...
            Thread.Sleep(150);
            //Radio IP should always be first item in td for given table...will need to handle if multiple radios are presented. 
            //again need to test via ping if radio is up or not. 
            //need to determine radio type via webpage DOM 
            var RadioTable = browser.FindElementByTagName("td");
            Console.WriteLine(RadioTable.Text);

            //logic to ping radio


            browser.Navigate().GoToUrl(RadioTable.Text.ToString());
            Console.WriteLine(browser.Url);

            // logic to determine Radio type / transfer browser along proper channel here: 
            //ePMP test radio: 172.20.70.174
            //450 test radio: 172.16.98.161
            //WiMax test radio: 172.22.94.16
            //VL ? debating...

            string RadioFourFiftey = "quickform";
            string RadioWimax = "img_bg";
            string Radio_ePMP = "top-level-menu";
            if (RadioType(RadioFourFiftey, browser) == true)
            {
                ScrapeFourFifty(browser);

            }
            else if (RadioType(Radio_ePMP, browser) == true)
            {
                Scrape_ePMP(browser);
            }
            else if (RadioType(RadioWimax, browser) == true)
            {
                ScrapeWimax(browser);
            }
            else
            {
                //If going for VL, here would be the place to attempt telnet, but 450 & ePMP will accept telnet connection too. 
                Console.WriteLine("Radio was not found or is not a 450, ePMP or 320. Try again or search manually");
            }

        }
        public static bool RadioType(string id, ChromeDriver browser)
        {
            try
            {
                browser.FindElementById(id);
            }
            catch (NoSuchElementException e)
            {
                Console.WriteLine($"Radio was not found or is not a 450, ePMP or 320 \nError code: {e.ToString()}");
                return false; 
            }
            return true; 
        }


        //450 Radio scraping logic
        public static string ScrapeFourFifty(ChromeDriver browser)
        {


            /*Shit list:  
           #1 finding/Navigating to correct page...might try via <a> element array
           #2 Rebooting the radio, button does not load in headless chrome browser .
           #3 who knows, but I'm sure I'll find something. 
           */

            /*
            IDEA FOR NAVIGATING 450 Radio
            for whatever freaking reason, 450's combine this data as it's webpages
            order: /main.cgi?mac_esn=0a003e433075 &catindex=1& pageindex=0& ession=1481765933
            alt method of navigation would be to build the url's each time, just change the values to garuntee we get where we are wanting to go. 
            <input type="hidden" name="mac_esn" value="0a003e41ad1e" id="mac_esn">
            <input type="hidden" name="catindex" value="1" id="catindex">
            <input type="hidden" name="pageindex" value="0" id="pageindex">
            <input type="hidden" name="Session" value="1191391529" id="Session">
            */

            //Login to Radio
            var userId = browser.FindElementById("CanopyUsername");
            var Radiopassword = browser.FindElementById("CanopyPassword");
            var login = browser.FindElementById("loginbutton");
            userId.SendKeys("admin");
            Radiopassword.SendKeys("amatech1");
            login.Submit();

            //Gather Stats
            var FourFifty_Uptime = browser.FindElementById("UpTime").Text;
            var FourFifty_EthernetStats = browser.FindElementById("LinkStatusMain").Text;
            var FourFifty_Rssi = browser.FindElementById("PowerLevelOFDM").Text;
            var FourFifty_Snr = browser.FindElementById("SignalToNoiseRatioSM").Text;

            //Tested and working code to find & reboot 450 Radio
            browser.FindElementsByClassName("menu")[1].Click();
            Thread.Sleep(100);
            var rebootTestForm = browser.FindElementById("reboot");
            rebootTestForm.Click();

            //Return Stats
            var FourFifty_Stats = $"Uptime: {FourFifty_Uptime}\n";
            FourFifty_Stats += $"RSSI: {FourFifty_Rssi}\n";
            FourFifty_Stats += $"SNR: {FourFifty_Snr}\n";
            FourFifty_Stats += $"Ethernet Status: {FourFifty_EthernetStats}";
            return FourFifty_Stats;
	    }
	    

	 // ePMP Radio Scraping Logic: 
	    
        public static string Scrape_ePMP(ChromeDriver browser, string RadioIP)
        {
            browser.Navigate().GoToUrl(RadioIP);
            var userId = browser.FindElementByName("username");
            var pwd = browser.FindElementByName("password");
            var login = browser.FindElementById("loginBtn");
            
            userId.SendKeys("admin");
            pwd.SendKeys("amatech1");
            login.Click();
            /******************************************************************************************
             * Note for Error handling: 
             * Max # of users: <span class="error-text">Maximum number of users reached.</span>
             * Wrong Username/Password: <span class="error-text">Wrong username or password</span>
            ******************************************************************************************/
            


            //Have to set explicit waits for ePMP DOM to load...wireless connections be slow
            Thread.Sleep(500);
            var ePMPRssi = browser.FindElementById("dl_rssi").GetAttribute("title");
            var ePMPSNR = browser.FindElementById("dl_snr").GetAttribute("title");
            //var ePMP_EthernetStatus = browser.FindElementsById("alert-success").GetAttribute("title");
            var ePMPUptime = browser.FindElementById("sys_uptime").GetAttribute("title");
            var ePMP_DlMod = browser.FindElementById("dl_mcs_mode").GetAttribute("title");
            var ePMP_ULMod = browser.FindElementById("ul_mcs_mode").GetAttribute("title");

            // reboot req handling popup
            // ref stackoverflow: https://stackoverflow.com/questions/12744576/selenium-c-sharp-accept-confirm-box
            string pageSrc = browser.PageSource;
            
            //find & click reboot button, currently not visible in DOM 
            browser.FindElementByClassName("navbar-toggle").Click();
            Thread.Sleep(500);
            browser.FindElementById("reboot_device").Click();
            

            string JSAlertError = null;
            string RadioStats = null;
            //Handle pop up asking us if we're sure we want to reboot, yes we are. 
            try
            {
                var handleAlert = browser.SwitchTo().Alert();

                handleAlert.Accept();
            }
            catch (Exception e)
            {
                JSAlertError = e.ToString();
                Console.WriteLine(e.StackTrace);
            }

            //Title's in ePMP radio are "preformated", will concantenate strings together and display. 
            RadioStats = $"{ePMPUptime.ToString()}\n";
            RadioStats += $"{ePMPRssi.ToString()}\n";
            RadioStats += $"{ePMPSNR.ToString()}\n";
            RadioStats += $"{ePMP_DlMod.ToString()}\n";
            RadioStats += $"{ePMP_ULMod.ToString()}\n";

            Console.WriteLine(RadioStats);
            Console.WriteLine("ePMP Complete...");
            return RadioStats; 
            

             //  return scrapedData;
        }
        public static string ScrapeWimax(ChromeDriver browser)
        {
            /* WIMAX RADIOS
	        test IP: 172.28.151.210
	        Shit list: 
	        Rebooting: can call via Js on ajaxReboot();
	        */

	        var login = browser.FindElementByName("login_form");
	        var usrID = browser.FindElementByName("username");
	        var pwd = browser.FindElementByName("passwd"); 


	        var UpTime = browser.FindElementById("ssUptime").Text;
	        var Rssi = browser.FindElementById("RSSI").Text;
	        var Cinr = browser.FindElementById("CINR").Text;
	        var ulMod = browser.FindElementById("ulMod");
	        var dlMod = browser.FindElementById("dlMod");




	        // string builder: 

	        var WimaxRadio = $"Uptime: {UpTime}\n";
	        WimaxRadio += $"RSSI: {Rssi}\n";
	        WimaxRadio += $"CINR: {Cinr}\n";
	        WimaxRadio += $"Mods Up/Dwn: {ulMod} / {dlMod}\n Rebooted Radio";
	        return WimaxRadio; 
        }
    }
}
