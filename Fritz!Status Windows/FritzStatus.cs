using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace Fritz_Status {
	public enum FritzConnectionStatus {
		Connected,
		Training,
		NotConnected,
		Unknown
	}

	public class DslOverview {
		public string dslam { get; set; }
		public Line[] line { get; set; }
		public string ds_rate { get; set; }
		public string us_rate { get; set; }
	}

	public class Line {
		public string mode { get; set; }
		public string train_state { get; set; }
		public string time { get; set; }
		public string pic { get; set; } // FRITZ!OS < 6.50 ?
		public string state { get; set; } // FRITZ!OS >= 6.50 ?

		public FritzConnectionStatus GetConnectionStatus() {
			if (!pic.IsNullOrWhiteSpace()) {
				if(pic.Contains("rot")) {
					return FritzConnectionStatus.NotConnected;
				} else if (pic.Contains("gelb")) {
					return FritzConnectionStatus.Training;
				} else if(pic.Contains("gruen")) {
					return FritzConnectionStatus.Connected;
				} else {
					return FritzConnectionStatus.Unknown;
				}
			} else if (!state.IsNullOrWhiteSpace()) {
				switch (state) {
					case "error":
						return FritzConnectionStatus.NotConnected;
					case "training":
						return FritzConnectionStatus.Training;
					case "ready":
						return FritzConnectionStatus.Connected;
					default:
						return FritzConnectionStatus.Unknown;
				}
			} else {
				return FritzConnectionStatus.Unknown;
			}
		}
	}

	public struct BoxInfo {
		public string BoxName;
		public string BoxOS;
		public string BoxNumber;
		public bool IsNull; // Too lazy to use Nullables
	}


	public class FritzStatus {
		const string FRITZ_SID = "http://fritz.box/login_sid.lua";
		const string FRITZ_HOME = "http://fritz.box/home/home.lua";
		const string FRITZ_FBOS = "http://fritz.box/home/pp_fbos.lua";
		const string FRITZ_OVERVIEW = "http://fritz.box/internet/dsl_overview.lua?useajax=1&action=get_data";

		public const string INVALID_SID = "0000000000000000";

		const string SID_REGEX = "<SID>(.*)</SID>";
		const string CHALLENGE_REGEX = "<Challenge>(.*)</Challenge>";

		const string BOXNAME_REGEX = "<div>\n<p>(.*)\n";
		const string BOX_OS_REGEX = "</p>\n<p>(.*)\n";
		const string BOX_NUMBER_REGEX = "(\\d+)";

		Regex sidRegex = new Regex(SID_REGEX, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
		Regex challengeRegex = new Regex(CHALLENGE_REGEX, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		Regex boxNameRegex = new Regex(BOXNAME_REGEX, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
		Regex boxOSRegex = new Regex(BOX_OS_REGEX, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
		Regex boxNumberRegex = new Regex(BOX_NUMBER_REGEX, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public string SessionID = "";

		public async Task<string> GetSessionID() {
			HttpWebRequest request = WebRequest.CreateHttp(FRITZ_SID);
			request.Timeout = 5000;

			try {
				HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());

				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
					string responseText = reader.ReadToEnd();

					var sid = sidRegex.Match(responseText).Groups[1].Value;
					var challenge = challengeRegex.Match(responseText).Groups[1].Value;

					return sid;
				}
			} catch (Exception ex) {
				Debug.WriteLine(ex);
				return null;
			}
		}

		public async Task<BoxInfo> GetBoxInfo() {
			var uriBuilder = new UriBuilder(FRITZ_FBOS);
			var query = HttpUtility.ParseQueryString(uriBuilder.Query);
			query["sid"] = this.SessionID;
			uriBuilder.Query = query.ToString();
			Debug.WriteLine(uriBuilder.ToString());

			HttpWebRequest request = WebRequest.CreateHttp(uriBuilder.ToString());
			request.Timeout = 7500;

			try {
				HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());

				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
					string responseText = reader.ReadToEnd();

					var boxName = boxNameRegex.Match(responseText).Groups[1].Value;
					var boxOS = boxOSRegex.Match(responseText).Groups[1].Value;
					var boxNumber = boxNumberRegex.Match(boxName).Groups[1].Value;

					return new BoxInfo() {
						BoxName = boxName,
						BoxOS = boxOS,
						BoxNumber = boxNumber
					};
				}
			} catch (Exception ex) {
				Debug.WriteLine(ex);
				return new BoxInfo() { IsNull = true };
			}
		}

		public async Task<DslOverview> GetStatus() {
			HttpWebRequest request = WebRequest.CreateHttp(FRITZ_OVERVIEW);
			request.Timeout = 7500;

			try {
				HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());

				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
					string responseText = reader.ReadToEnd();

					JavaScriptSerializer jss = new JavaScriptSerializer();
					DslOverview overview = jss.Deserialize<DslOverview>(responseText);

					return overview;
				}
			} catch (Exception ex) {
				Debug.WriteLine(ex);
				return null;
			}
		}


	}
}
