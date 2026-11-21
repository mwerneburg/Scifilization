// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;

namespace CivOne.Leaders
{
	public abstract class BaseLeader : BaseInstance, ILeader
	{
		private string _defaultName = null!;
		private string DefaultName
		{
			get
			{
				foreach (LeaderModification modification in Modifications)
				{
					if (!modification.Name.HasValue) continue;
					_defaultName = modification.Name.Value;
				}
				return _defaultName;
			}
			set => _defaultName = value;
		}
		
		public string Name { get; set; }

		private Picture _portraitSmall;
		private Picture? _pngPortrait;

		protected abstract Leader Leader { get; }

		private static string? LeaderArtPath(string name)
		{
			try
			{
				string file = name.ToLower()
					.Replace('.', '_').Replace(' ', '_') + ".png";
				string path = Path.Combine(Settings.Instance.DataDirectory, "leader_art", file);
				return File.Exists(path) ? path : null;
			}
			catch { return null; }
		}

		private Picture? LoadPngPortrait()
		{
			if (_pngPortrait is not null) return _pngPortrait;
			string? path = LeaderArtPath(Name);
			if (path is null) return null;
			byte[] rgba = PngFile.ReadRgba(path, out int w, out int h);
			if (rgba is null) return null;
			using Palette pal = Common.DefaultPalette;
			CassetteTheme.ApplyTo(pal);
			byte[,] idx = PngFile.ToIndices(rgba, w, h, pal);
			var pic = new Picture(w, h, pal);
			for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
				pic.Bitmap[x, y] = idx[y, x];
			_pngPortrait = pic;
			return _pngPortrait;
		}

		private IBitmap? _modifiedPicture, _modifiedPortraitSmall;
		public Picture GetPortrait(FaceState state = FaceState.Neutral)
		{
			Picture? png = LoadPngPortrait();
			if (png is not null) return png;

			if (_modifications.ContainsKey(Leader))
			{
				if (_modifiedPicture is null)
				{
					_modifiedPicture = _modifications[Leader].LastOrDefault(x => x.Portrait is not null && x.Portrait.GifToBitmap() is not null)?.Portrait.GifToBitmap();
				}

				if (_modifiedPicture is not null && _modifiedPicture.Width() == 139 && _modifiedPicture.Height() == 133)
				{
					return _modifiedPicture.MakePalette(64, 16);
				}
			}

			return new Picture(Free.Instance.Portrait(139, 133), Common.GetPalette256);
		}

		public Picture PortraitSmall
		{
			get
			{
				if (_modifications.ContainsKey(Leader))
				{
					if (_modifiedPortraitSmall is null)
					{
						_modifiedPortraitSmall = _modifications[Leader].LastOrDefault(x => x.PortraitSmall is not null && x.PortraitSmall.GifToBitmap() is not null)?.PortraitSmall.GifToBitmap();
					}

					if (_modifiedPortraitSmall is not null && _modifiedPortraitSmall.Width() == 27 && _modifiedPortraitSmall.Height() == 33)
					{
						return new Picture(_modifiedPortraitSmall.MatchColours(Common.GetPalette256, 1, 255), Common.GetPalette256);
					}
				}
				return _portraitSmall;
			}
		}
		
		// Declared by leaders with a deliberate character; otherwise derived from the
		// personality enums (see Doctrine.FromTraits), so every existing leader keeps
		// broadly the behaviour it had while gaining its own thresholds.
		private Doctrine? _doctrine;
		public virtual Doctrine Doctrine =>
			_doctrine ??= Doctrine.FromTraits(Name, Development, Aggression, Militarism);

		private AggressionLevel _aggression = AggressionLevel.Normal;
		public AggressionLevel Aggression
		{
			get
			{
				foreach (LeaderModification modification in Modifications)
				{
					if (!modification.Aggression.HasValue) continue;
					_aggression = modification.Aggression.Value;
				}
				return _aggression;
			}
			set => _aggression = value;
		}

		private DevelopmentLevel _development = DevelopmentLevel.Normal;
		public DevelopmentLevel Development
		{
			get
			{
				foreach (LeaderModification modification in Modifications)
				{
					if (!modification.Development.HasValue) continue;
					_development = modification.Development.Value;
				}
				return _development;
			}
			set => _development = value;
		}

		private MilitarismLevel _militarism = MilitarismLevel.Normal;
		public MilitarismLevel Militarism
		{
			get
			{
				foreach (LeaderModification modification in Modifications)
				{
					if (!modification.Militarism.HasValue) continue;
					_militarism = modification.Militarism.Value;
				}
				return _militarism;
			}
			set => _militarism = value;
		}
		
		private static Dictionary<Leader, List<LeaderModification>> _modifications = new Dictionary<Leader, List<LeaderModification>>();
		internal static void LoadModifications()
		{
			_modifications.Clear();

			LeaderModification[] modifications = Reflect.GetModifications<LeaderModification>().ToArray();
			if (modifications.Length == 0) return;

			Log("Applying leader modifications");

			foreach (LeaderModification modification in modifications)
			{
				if (!_modifications.ContainsKey(modification.Leader))
					_modifications.Add(modification.Leader, new List<LeaderModification>());
				_modifications[modification.Leader].Add(modification);
			}

			Log("Finished applying leader modifications");
		}
		public IEnumerable<LeaderModification> Modifications => _modifications.ContainsKey(Leader) ? _modifications[Leader].ToArray() : new LeaderModification[0];

		// The DOS leaders once took their KINGnn.PIC name here; only they got a face on the
		// conquest screen, and everyone else a blank box.
		protected BaseLeader(string name)
		{
			DefaultName = name;
			Name = DefaultName;
			_portraitSmall = new Picture(Free.Instance.Portrait(27, 33), Common.GetPalette256);
		}
	}
}