﻿// Copyright 2009-2014 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Object representing persistent state ("record") of a player, online or offline.
    /// There is exactly one PlayerInfo object for each known Minecraft account. All data is stored in the PlayerDB. </summary>
    public sealed partial class PlayerInfo : IClassy {
        internal const int MinFieldCount = 24;


        /// <summary> Player's account name. </summary>
        [NotNull]
        public string Name { get; internal set; }

        public string ClassyName {
            get {
                StringBuilder sb = new StringBuilder();
                if( ConfigKey.RankColorsInChat.Enabled() ) {
                    sb.Append( Rank.Color );
                }
                if( DisplayedName != null ) {
                    if( ConfigKey.RankPrefixesOnDisplayedNames.Enabled() ) {
                        sb.Append( Rank.Prefix );
                    }
                    sb.Append( DisplayedName );
                } else {
                    if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        sb.Append( Rank.Prefix );
                    }
                    sb.Append( Name );
                }
                if( IsBanned ) {
                    sb.Append( Color.Red ).Append( '*' );
                } else if( IsFrozen ) {
                    sb.Append( Color.Blue ).Append( '*' );
                }
                return sb.ToString();
            }
        }

        /// <summary> If set, replaces Name when printing name in chat. </summary>
        [CanBeNull]
        public string DisplayedName;

        [CanBeNull]
        public string Email;

        /// <summary> Player's unique numeric ID. Issued on first join. </summary>
        public int ID;

        /// <summary> First time the player ever logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime FirstLoginDate;

        /// <summary> Most recent time the player logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastLoginDate;

        /// <summary> Last time the player has been seen online (last logout), UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastSeen;

        /// <summary> Reason for leaving the server last time. </summary>
        public LeaveReason LeaveReason;

        /// <summary> Minecraft.net account type (paid/free/unknown). </summary>
        public AccountType AccountType;


        /// <summary> Begins to asynchronously check player's account type. </summary>
        public void CheckAccountType() {
            if( AccountType != AccountType.Paid ) {
                Scheduler.NewBackgroundTask( CheckPaidStatusCallback ).RunOnce( this, TimeSpan.Zero );
            }
        }


        static void CheckPaidStatusCallback( SchedulerTask task ) {
            PlayerInfo info = (PlayerInfo)task.UserState;
        }


        #region Rank

        /// <summary> Player's current rank. </summary>
        [NotNull]
        public Rank Rank { get; internal set; }

        /// <summary> Player's previous rank.
        /// May be null if player has never been promoted/demoted before. </summary>
        [CanBeNull]
        public Rank PreviousRank;

        /// <summary> Date of the most recent promotion/demotion, UTC.
        /// May be DateTime.MinValue if player has never been promoted/demoted before. </summary>
        public DateTime RankChangeDate;

        /// <summary> Name of the player or entity who most recently promoted/demoted this player. May be null or empty. </summary>
        [CanBeNull]
        public string RankChangedBy;

        /// <summary> Decorated name of the player or entity who most recently changed rank of this player.
        /// Returns raw RankChangedBy value if it's not a recognized player name.
        /// Returns '?' if RankChangedBy is null or empty. </summary>
        [NotNull]
        public string RankChangedByClassy {
            get {
                return PlayerDB.FindExactClassyName( RankChangedBy );
            }
        }

        /// <summary> Reason given for the most recent promotion/demotion. May be null or empty. </summary>
        [CanBeNull]
        public string RankChangeReason;

        /// <summary> Type of the most recent promotion/demotion. </summary>
        public RankChangeType RankChangeType;

        #endregion


        #region Bans

        /// <summary> Player's current BanStatus: Banned, NotBanned, or Exempt. </summary>
        public BanStatus BanStatus;

        /// <summary> Returns whether player is name-banned or not. </summary>
        public bool IsBanned {
            get { return BanStatus == BanStatus.Banned; }
        }

        /// <summary> Date of most recent ban, UTC. May be DateTime.MinValue if player was never banned. </summary>
        public DateTime BanDate;

        /// <summary> Name of the player or entity who most recently banned this player. May be null or empty. </summary>
        [CanBeNull]
        public string BannedBy;

        /// <summary> Decorated name of the player or entity who most recently banned this player.
        /// Returns raw BannedBy value if it's not a recognized player name.
        /// Returns '?' if BannedBy is null or empty. </summary>
        [NotNull]
        public string BannedByClassy {
            get {
                return PlayerDB.FindExactClassyName( BannedBy );
            }
        }

        /// <summary> Reason given for the most recent ban. May be null or empty. </summary>
        [CanBeNull]
        public string BanReason;

        /// <summary> Date of most recent unban, UTC. May be DateTime.MinValue if player was never unbanned. </summary>
        public DateTime UnbanDate;

        /// <summary> Name of the player or entity who most recently unbanned this player. May be null or empty. </summary>
        [CanBeNull]
        public string UnbannedBy;

        /// <summary> Decorated name of the player or entity who most recently unbanned this player.
        /// Returns raw UnbannedBy value if it's not a recognized player name.
        /// Returns '?' if UnbannedBy is null or empty. </summary>
        [NotNull]
        public string UnbannedByClassy {
            get {
                return PlayerDB.FindExactClassyName( UnbannedBy );
            }
        }

        /// <summary> Reason given for the most recent unban. May be null or empty. </summary>
        [CanBeNull]
        public string UnbanReason;

        /// <summary> Date of most recent failed attempt to log in, UTC. </summary>
        public DateTime LastFailedLoginDate;

        /// <summary> IP from which player most recently tried (and failed) to log in, UTC. </summary>
        [NotNull]
        public IPAddress LastFailedLoginIP = IPAddress.None;

        #endregion


        #region Stats

        /// <summary> Total amount of time the player spent on this server. </summary>
        public TimeSpan TotalTime;

        /// <summary> Total number of blocks manually built or painted by the player. </summary>
        public int BlocksBuilt;

        /// <summary> Total number of blocks manually deleted by the player. </summary>
        public int BlocksDeleted;

        /// <summary> Total number of blocks modified using draw and copy/paste commands. </summary>
        public long BlocksDrawn;

        /// <summary> Number of sessions/logins. </summary>
        public int TimesVisited;

        /// <summary> Total number of chat messages written. </summary>
        public int MessagesWritten;

        /// <summary> Number of kicks issues by this player. </summary>
        public int TimesKickedOthers;

        /// <summary> Number of bans issued by this player. </summary>
        public int TimesBannedOthers;

        #endregion


        #region Kicks

        /// <summary> Number of times that this player has been manually kicked. </summary>
        public int TimesKicked;

        /// <summary> Date of the most recent kick.
        /// May be DateTime.MinValue if the player has never been kicked. </summary>
        public DateTime LastKickDate;

        /// <summary> Name of the player or entity who most recently kicked this player. May be null or empty. </summary>
        [CanBeNull]
        public string LastKickBy;

        /// <summary> Decorated name of the player or entity who most recently kicked this player.
        /// Returns raw LastKickBy value if it's not a recognized player name.
        /// Returns '?' if LastKickBy is null or empty. </summary>
        [NotNull]
        public string LastKickByClassy {
            get {
                return PlayerDB.FindExactClassyName( LastKickBy );
            }
        }

        /// <summary> Reason given for the most recent kick. May be null or empty. </summary>
        [CanBeNull]
        public string LastKickReason;

        #endregion


        #region Freeze And Mute

        /// <summary> Whether this player is currently frozen. </summary>
        public bool IsFrozen;

        /// <summary> Date of the most recent freezing of this player.
        /// May be DateTime.MinValue of the player has never been frozen. </summary>
        public DateTime FrozenOn;

        /// <summary> Name of the player or entity who most recently froze this player. May be null or empty. </summary>
        [CanBeNull]
        public string FrozenBy;

        /// <summary> Decorated name of the player or entity who most recently froze this player.
        /// Returns raw FrozenBy value if it's not a recognized player name.
        /// Returns '?' if FrozenBy is null or empty. </summary>
        [NotNull]
        public string FrozenByClassy {
            get {
                return PlayerDB.FindExactClassyName( FrozenBy );
            }
        }

        /// <summary> Whether this player is currently muted. </summary>
        public bool IsMuted {
            get {
                return DateTime.UtcNow < MutedUntil;
            }
        }

        /// <summary> Date until which the player is muted. If the date is in the past, player is NOT muted. </summary>
        public DateTime MutedUntil;

        /// <summary> Name of the player or entity who most recently muted this player. May be null or empty. </summary>
        [CanBeNull]
        public string MutedBy;

        /// <summary> Decorated name of the player or entity who most recently muted this player.
        /// Returns raw MutedBy value if it's not a recognized player name.
        /// Returns '?' if MutedBy is null or empty. </summary>
        [NotNull]
        public string MutedByClassy {
            get {
                return PlayerDB.FindExactClassyName( MutedBy );
            }
        }

        #endregion


        /// <summary> Whether the player is currently online.
        /// Another way to check online status is to check if PlayerObject is null. </summary>
        public bool IsOnline { get; private set; }

        /// <summary> If player is online, Player object associated with the session.
        /// If player is offline, null. </summary>
        [CanBeNull]
        public Player PlayerObject { get; private set; }

        /// <summary> Whether the player is currently hidden.
        /// Use Player.CanSee() method to check visibility to specific observers. </summary>
        public bool IsHidden;

        /// <summary> For offline players, last IP used to successfully log in.
        /// For online players, current IP. </summary>
        [NotNull]
        public IPAddress LastIP;


        #region Constructors and Serialization

        internal PlayerInfo( int id ) {
            ID = id;
        }

        PlayerInfo() {
            // reset everything to defaults
            LastIP = IPAddress.None;
            RankChangeDate = DateTime.MinValue;
            BanDate = DateTime.MinValue;
            UnbanDate = DateTime.MinValue;
            LastFailedLoginDate = DateTime.MinValue;
            FirstLoginDate = DateTime.MinValue;
            LastLoginDate = DateTime.MinValue;
            TotalTime = TimeSpan.Zero;
            RankChangeType = RankChangeType.Default;
            LastKickDate = DateTime.MinValue;
            LastSeen = DateTime.MinValue;
            BannedUntil = DateTime.MinValue;
            FrozenOn = DateTime.MinValue;
            MutedUntil = DateTime.MinValue;
            BandwidthUseMode = BandwidthUseMode.Default;
            LastModified = DateTime.UtcNow;
        }

        // fabricate info for an unrecognized player
        public PlayerInfo( [NotNull] string name, [NotNull] Rank rank,
                           bool setLoginDate, RankChangeType rankChangeType )
            : this() {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            Name = name;
            Rank = rank;
            if( setLoginDate ) {
                FirstLoginDate = DateTime.UtcNow;
                LastLoginDate = FirstLoginDate;
                LastSeen = FirstLoginDate;
                TimesVisited = 1;
            }
            RankChangeType = rankChangeType;
        }


        // generate blank info for a new player
        public PlayerInfo( [NotNull] string name, [NotNull] IPAddress lastIP, [NotNull] Rank startingRank )
            : this() {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            FirstLoginDate = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            LastLoginDate = DateTime.UtcNow;
            Rank = startingRank;
            Name = name;
            ID = PlayerDB.GetNextID();
            LastIP = lastIP;
        }

        #endregion


        #region Loading

        static readonly NumberFormatInfo NumberFormatter = CultureInfo.InvariantCulture.NumberFormat;
        internal static PlayerInfo LoadFormat2( string[] fields ) {
            if( fields.Length < 44 ) {
                throw new FormatException( "PlayerInfo record did not contain all the expected information. " +
                                           "This record, or maybe the whole file, may be corrupted." );
            }

            if( !Player.IsValidPlayerName( fields[0] ) ) {
                throw new FormatException( "Unacceptable player name" );
            }
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            DateTimeUtil.TryParseDateTime( fields[3], ref info.RankChangeDate );
            if( fields[4].Length > 0 ) info.RankChangedBy = PlayerDB.Unescape( fields[4] );

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( DateTimeUtil.TryParseDateTime( fields[6], ref info.BanDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = PlayerDB.Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.BanReason = PlayerDB.Unescape( fields[10] );
            }

            // unban information
            if( DateTimeUtil.TryParseDateTime( fields[8], ref info.UnbanDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = PlayerDB.Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.UnbanReason = PlayerDB.Unescape( fields[11] );
            }

            // failed logins
            DateTimeUtil.TryParseDateTime( fields[12], ref info.LastFailedLoginDate );

            if( fields[13].Length < 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) {
                // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            // skip 14

            DateTimeUtil.TryParseDateTime( fields[15], ref info.FirstLoginDate );

            // login/logout times
            DateTimeUtil.TryParseDateTime( fields[16], ref info.LastLoginDate );
            DateTimeUtil.TryParseTimeSpan( fields[17], out info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], NumberStyles.Integer, NumberFormatter, out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], NumberStyles.Integer, NumberFormatter, out info.BlocksDeleted );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[20], NumberStyles.Integer, NumberFormatter, out info.TimesVisited );
            if( fields[21].Length > 0 ) Int32.TryParse( fields[21], NumberStyles.Integer, NumberFormatter, out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = PlayerDB.Unescape( fields[25] );
            Int32.TryParse( fields[26], NumberStyles.Integer, NumberFormatter, out info.TimesKicked );
            Int32.TryParse( fields[27], NumberStyles.Integer, NumberFormatter, out info.TimesKickedOthers );
            Int32.TryParse( fields[28], NumberStyles.Integer, NumberFormatter, out info.TimesBannedOthers );

            info.ID = Int32.Parse( fields[29] );
            if( info.ID < 256 )
                info.ID = PlayerDB.GetNextID();

            byte rankChangeTypeCode;
            if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                switch( info.RankChangeType ) {
                    case RankChangeType.AutoDemoted:
                    case RankChangeType.AutoPromoted:
                    case RankChangeType.Default:
                    case RankChangeType.Demoted:
                    case RankChangeType.Promoted:
                        break;
                    default:
                        info.GuessRankChangeType();
                        break;
                }
            } else {
                info.GuessRankChangeType();
            }

            DateTimeUtil.TryParseDateTime( fields[31], ref info.LastKickDate );
            if( !DateTimeUtil.TryParseDateTime( fields[32], ref info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }
            Int64.TryParse( fields[33], NumberStyles.Integer, NumberFormatter, out info.BlocksDrawn );

            if( fields[34].Length > 0 ) info.LastKickBy = PlayerDB.Unescape( fields[34] );
            if( fields[35].Length > 0 ) info.LastKickReason = PlayerDB.Unescape( fields[35] );

            DateTimeUtil.TryParseDateTime( fields[36], ref info.BannedUntil );
            info.IsFrozen = ( fields[37] == "f" );
            if( fields[38].Length > 0 ) info.FrozenBy = PlayerDB.Unescape( fields[38] );
            DateTimeUtil.TryParseDateTime( fields[39], ref info.FrozenOn );
            DateTimeUtil.TryParseDateTime( fields[40], ref info.MutedUntil );
            if( fields[41].Length > 0 ) info.MutedBy = PlayerDB.Unescape( fields[41] );
            info.Password = PlayerDB.Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            byte bandwidthUseModeCode;
            if( Byte.TryParse( fields[44], NumberStyles.Integer, NumberFormatter, out bandwidthUseModeCode ) ) {
                info.BandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                switch( info.BandwidthUseMode ) {
                    case BandwidthUseMode.High:
                    case BandwidthUseMode.Low:
                    case BandwidthUseMode.Normal:
                    case BandwidthUseMode.VeryHigh:
                    case BandwidthUseMode.VeryLow:
                        break;
                    default:
                        info.BandwidthUseMode = BandwidthUseMode.Default;
                        break;
                }
            } else {
                info.BandwidthUseMode = BandwidthUseMode.Default;
            }

            if( fields.Length > 45 && fields[45] == "h" ) {
                info.IsHidden = info.Rank.Can( Permission.Hide );
            }
            if( fields.Length > 46 ) {
                DateTimeUtil.TryParseDateTime( fields[46], ref info.LastModified );
            }
            if( fields.Length > 47 && fields[47].Length > 0 ) {
                info.DisplayedName = PlayerDB.Unescape( fields[47] );
            }
            if( fields.Length > 48 ) {
                byte accountTypeCode;
                if( Byte.TryParse( fields[48], NumberStyles.Integer, NumberFormatter, out accountTypeCode ) ) {
                    info.AccountType = (AccountType)accountTypeCode;
                    if( !Enum.IsDefined( typeof( AccountType ), accountTypeCode ) ) {
                        info.AccountType = AccountType.Unknown;
                    }
                }
            }
            if( fields.Length > 49 && fields[49].Length > 0 ) {
                info.Email = PlayerDB.Unescape( fields[49] );
            }

            // date consistency checks
            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat1( string[] fields ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTimeLegacy( ref info.RankChangeDate );
            if( fields[4].Length > 0 ) info.RankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTimeLegacy( ref info.BanDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = PlayerDB.Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.BanReason = PlayerDB.Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTimeLegacy( ref info.UnbanDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = PlayerDB.Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.UnbanReason = PlayerDB.Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTimeLegacy( ref info.LastFailedLoginDate );

            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            // skip 14
            fields[15].ToDateTimeLegacy( ref info.FirstLoginDate );

            // login/logout times
            fields[16].ToDateTimeLegacy( ref info.LastLoginDate );
            fields[17].ToTimeSpanLegacy( ref info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = PlayerDB.Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.TimesKicked );
            Int32.TryParse( fields[27], out info.TimesKickedOthers );
            Int32.TryParse( fields[28], out info.TimesBannedOthers );

            info.ID = Int32.Parse( fields[29] );
            if( info.ID < 256 )
                info.ID = PlayerDB.GetNextID();

            byte rankChangeTypeCode;
            if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTimeLegacy( ref info.LastKickDate );
            if( !fields[32].ToDateTimeLegacy( ref info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }
            Int64.TryParse( fields[33], out info.BlocksDrawn );

            if( fields[34].Length > 0 ) info.LastKickBy = PlayerDB.Unescape( fields[34] );
            if( fields[34].Length > 0 ) info.LastKickReason = PlayerDB.Unescape( fields[35] );

            fields[36].ToDateTimeLegacy( ref info.BannedUntil );
            info.IsFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.FrozenBy = PlayerDB.Unescape( fields[38] );
            fields[39].ToDateTimeLegacy( ref info.FrozenOn );
            fields[40].ToDateTimeLegacy( ref info.MutedUntil );
            if( fields[41].Length > 0 ) info.MutedBy = PlayerDB.Unescape( fields[41] );
            info.Password = PlayerDB.Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            byte bandwidthUseModeCode;
            if( Byte.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.BandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.BandwidthUseMode = BandwidthUseMode.Default;
                }
            } else {
                info.BandwidthUseMode = BandwidthUseMode.Default;
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.IsHidden = false;
                } else {
                    info.IsHidden = info.Rank.Can( Permission.Hide );
                }
            }

            // date consistency checks
            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            return info;
        }


        void GuessRankChangeType() {
            if( PreviousRank != null ) {
                if( RankChangeReason == "~AutoRank" || RankChangeReason == "~AutoRankAll" || RankChangeReason == "~MassRank" ) {
                    if( PreviousRank > Rank ) {
                        RankChangeType = RankChangeType.AutoDemoted;
                    } else if( PreviousRank < Rank ) {
                        RankChangeType = RankChangeType.AutoPromoted;
                    }
                } else {
                    if( PreviousRank > Rank ) {
                        RankChangeType = RankChangeType.Demoted;
                    } else if( PreviousRank < Rank ) {
                        RankChangeType = RankChangeType.Promoted;
                    }
                }
            } else {
                RankChangeType = RankChangeType.Default;
            }
        }

        #endregion


        #region Saving

        internal void Serialize( StringBuilder sb ) {
            sb.Append( Name ).Append( ',' ); // 0
            if( !LastIP.Equals( IPAddress.None ) ) sb.Append( LastIP ); // 1
            sb.Append( ',' );

            sb.Append( Rank.FullName ).Append( ',' ); // 2
            RankChangeDate.ToUnixTimeString( sb ).Append( ',' ); // 3

            sb.AppendEscaped( RankChangedBy ).Append( ',' ); // 4

            switch( BanStatus ) {
                case BanStatus.Banned:
                    sb.Append( 'b' );
                    break;
                case BanStatus.IPBanExempt:
                    sb.Append( 'x' );
                    break;
            }
            sb.Append( ',' ); // 5

            BanDate.ToUnixTimeString( sb ).Append( ',' ); // 6
            sb.AppendEscaped( BannedBy ).Append( ',' ); // 7
            UnbanDate.ToUnixTimeString( sb ).Append( ',' ); // 8
            sb.AppendEscaped( UnbannedBy ).Append( ',' ); // 9
            sb.AppendEscaped( BanReason ).Append( ',' ); // 10
            sb.AppendEscaped( UnbanReason ).Append( ',' ); // 11

            LastFailedLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 12

            if( !LastFailedLoginIP.Equals( IPAddress.None ) ) sb.Append( LastFailedLoginIP ); // 13
            sb.Append( ',', 2 ); // skip 14

            FirstLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 15
            LastLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 16

            Player pObject = PlayerObject;
            if( pObject != null ) {
                sb.Append( TotalTime.Add( TimeSinceLastLogin ).ToSecondsString() ); // 17
            } else {
                sb.Append( TotalTime.ToSecondsString() ); // 17
            }
            sb.Append( ',' );

            if( BlocksBuilt > 0 ) sb.Digits( BlocksBuilt ); // 18
            sb.Append( ',' );

            if( BlocksDeleted > 0 ) sb.Digits( BlocksDeleted ); // 19
            sb.Append( ',' );

            sb.Digits( TimesVisited ).Append( ',' ); // 20


            if( MessagesWritten > 0 ) sb.Digits( MessagesWritten ); // 21
            sb.Append( ',', 3 ); // 22-23 no longer in use

            if( PreviousRank != null ) sb.Append( PreviousRank.FullName ); // 24
            sb.Append( ',' );

            sb.AppendEscaped( RankChangeReason ).Append( ',' ); // 25


            if( TimesKicked > 0 ) sb.Digits( TimesKicked ); // 26
            sb.Append( ',' );

            if( TimesKickedOthers > 0 ) sb.Digits( TimesKickedOthers ); // 27
            sb.Append( ',' );

            if( TimesBannedOthers > 0 ) sb.Digits( TimesBannedOthers ); // 28
            sb.Append( ',' );


            sb.Digits( ID ).Append( ',' ); // 29

            sb.Digits( (int)RankChangeType ).Append( ',' ); // 30


            LastKickDate.ToUnixTimeString( sb ).Append( ',' ); // 31

            if( IsOnline ) DateTime.UtcNow.ToUnixTimeString( sb ); // 32
            else LastSeen.ToUnixTimeString( sb );
            sb.Append( ',' );

            if( BlocksDrawn > 0 ) sb.Append( BlocksDrawn ); // 33
            sb.Append( ',' );

            sb.AppendEscaped( LastKickBy ).Append( ',' ); // 34
            sb.AppendEscaped( LastKickReason ).Append( ',' ); // 35

            BannedUntil.ToUnixTimeString( sb ); // 36

            if( IsFrozen ) {
                sb.Append( ',' ).Append( 'f' ).Append( ',' ); // 37
                sb.AppendEscaped( FrozenBy ).Append( ',' ); // 38
                FrozenOn.ToUnixTimeString( sb ).Append( ',' ); // 39
            } else {
                sb.Append( ',', 4 ); // 37-39
            }

            if( MutedUntil > DateTime.UtcNow ) {
                MutedUntil.ToUnixTimeString( sb ).Append( ',' ); // 40
                sb.AppendEscaped( MutedBy ).Append( ',' ); // 41
            } else {
                sb.Append( ',', 2 ); // 40-41
            }

            sb.AppendEscaped( Password ).Append( ',' ); // 42

            if( IsOnline ) sb.Append( 'o' ); // 43
            sb.Append( ',' );

            if( BandwidthUseMode != BandwidthUseMode.Default ) sb.Append( (byte)BandwidthUseMode ); // 44
            sb.Append( ',' );

            if( IsHidden ) sb.Append( 'h' ); // 45

            sb.Append( ',' );
            LastModified.ToUnixTimeString( sb ); // 46

            sb.Append( ',' );
            sb.AppendEscaped( DisplayedName ); // 47

            sb.Append( ',' );
            sb.Append( (byte)AccountType ); // 48

            sb.Append( ',' );
            if( Email != null ) {
                sb.AppendEscaped( Email );
            }
        }

        #endregion


        #region Update Handlers

        public void ProcessMessageWritten() {
            Interlocked.Increment( ref MessagesWritten );
            LastModified = DateTime.UtcNow;
        }


        public void DecrementMessageWritten() {
            Interlocked.Decrement( ref MessagesWritten );
            LastModified = DateTime.UtcNow;
        }


        public void ProcessLogin( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            LastIP = player.IP;
            LastLoginDate = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            Interlocked.Increment( ref TimesVisited );
            IsOnline = true;
            PlayerObject = player;
            LastModified = DateTime.UtcNow;
            if( FirstLoginDate == DateTime.MinValue ) FirstLoginDate = DateTime.UtcNow;
        }


        public void ProcessFailedLogin( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            LastFailedLoginDate = DateTime.UtcNow;
            LastFailedLoginIP = player.IP;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessLogout( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            TotalTime += player.LastActiveTime.Subtract( player.LoginTime );
            LastSeen = DateTime.UtcNow;
            IsOnline = false;
            PlayerObject = null;
            LeaveReason = player.LeaveReason;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessRankChange( [NotNull] Rank newRank, [NotNull] string changer, [CanBeNull] string reason, RankChangeType type ) {
            if( newRank == null ) throw new ArgumentNullException( "newRank" );
            if( changer == null ) throw new ArgumentNullException( "changer" );
            PreviousRank = Rank;
            Rank = newRank;
            RankChangeDate = DateTime.UtcNow;

            RankChangedBy = changer;
            RankChangeReason = reason;
            RankChangeType = type;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessBlockPlaced( byte type ) {
            if( type == 0 ) { // air
                Interlocked.Increment( ref BlocksDeleted );
            } else {
                Interlocked.Increment( ref BlocksBuilt );
            }
            LastModified = DateTime.UtcNow;
        }


        public void ProcessDrawCommand( int blocksDrawn ) {
            Interlocked.Add( ref BlocksDrawn, blocksDrawn );
            LastModified = DateTime.UtcNow;
        }


        internal void ProcessKick( [NotNull] Player kickedBy, [CanBeNull] string reason ) {
            if( kickedBy == null ) throw new ArgumentNullException( "kickedBy" );

            if( reason != null ) reason = reason.Trim( ' ' );
            if( reason != null && reason.Length == 0 ) reason = null;

            lock( actionLock ) {
                Interlocked.Increment( ref TimesKicked );
                Interlocked.Increment( ref kickedBy.Info.TimesKickedOthers );
                LastKickDate = DateTime.UtcNow;
                LastKickBy = kickedBy.Name;
                LastKickReason = reason;
                if( IsFrozen ) {
                    try {
                        Unfreeze( kickedBy, false, true );
                    } catch( PlayerOpException ex ) {
                        Logger.Log( LogType.Warning,
                                    "PlayerInfo.ProcessKick: {0}", ex.Message );
                    }
                }
                LastModified = DateTime.UtcNow;
            }
        }

        #endregion


        #region TimeSince_____ shortcuts

        public TimeSpan TimeSinceRankChange {
            get { return DateTime.UtcNow.Subtract( RankChangeDate ); }
        }

        public TimeSpan TimeSinceBan {
            get { return DateTime.UtcNow.Subtract( BanDate ); }
        }

        public TimeSpan TimeSinceUnban {
            get { return DateTime.UtcNow.Subtract( UnbanDate ); }
        }

        public TimeSpan TimeSinceFirstLogin {
            get { return DateTime.UtcNow.Subtract( FirstLoginDate ); }
        }

        public TimeSpan TimeSinceLastLogin {
            get { return DateTime.UtcNow.Subtract( LastLoginDate ); }
        }

        public TimeSpan TimeSinceLastKick {
            get { return DateTime.UtcNow.Subtract( LastKickDate ); }
        }

        public TimeSpan TimeSinceLastSeen {
            get { return DateTime.UtcNow.Subtract( LastSeen ); }
        }

        public TimeSpan TimeSinceFrozen {
            get { return DateTime.UtcNow.Subtract( FrozenOn ); }
        }

        public TimeSpan TimeMutedLeft {
            get {
                TimeSpan time = MutedUntil.Subtract( DateTime.UtcNow );
                if( time >= TimeSpan.Zero ) {
                    return time;
                }
                return TimeSpan.Zero;
            }
        }

        #endregion


        public override string ToString() {
            return String.Format( "PlayerInfo({0},{1})", Name, Rank.Name );
        }

        public bool Can( Permission permission ) {
            return Rank.Can( permission );
        }

        public bool Can( Permission permission, Rank rank ) {
            return Rank.Can( permission, rank );
        }


        #region Unfinished / Not Implemented

        /// <summary> Not implemented (IRC/server password hash). </summary>
        public string Password = ""; // TODO

        public DateTime LastModified; // TODO

        public BandwidthUseMode BandwidthUseMode; // TODO

        /// <summary> Not implemented (for temp bans). </summary>
        public DateTime BannedUntil; // TODO

        #endregion
    }


    /// <summary> Sorts PlayerInfo objects by relevance.
    /// Orders players by online/offline state first, then by rank, then by time-since-last-seen.
    /// Hidden players are listed with the offline players. </summary>
    public sealed class PlayerInfoComparer : IComparer<PlayerInfo> {
        readonly Player observer;

        public PlayerInfoComparer( Player observer ) {
            this.observer = observer;
        }

        public int Compare( PlayerInfo x, PlayerInfo y ) {
            Player xPlayer = x.PlayerObject;
            Player yPlayer = y.PlayerObject;
            bool xIsOnline = xPlayer != null && observer.CanSee( xPlayer );
            bool yIsOnline = yPlayer != null && observer.CanSee( yPlayer );

            if( !xIsOnline && yIsOnline ) {
                return 1;
            } else if( xIsOnline && !yIsOnline ) {
                return -1;
            }

            if( x.Rank == y.Rank ) {
                return Math.Sign( y.LastSeen.Ticks - x.LastSeen.Ticks );
            } else {
                return x.Rank.Index - y.Rank.Index;
            }
        }
    }
}