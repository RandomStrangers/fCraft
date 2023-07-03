/*
    Copyright 2015 MCForge
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using fCraft.Extensions;

namespace fCraft
{
    public class CpeExt
    {
        /// <summary> Name of the CPE extension (e.g. ExtPlayerList) </summary>
        public string Name;
        /// <summary> Highest version of this CPE extension supported by the server </summary>
        public byte ServerVersion;
        /// <summary> Highest version of this CPE extension supported by the client </summary>
        public byte ClientVersion;

        public const string ClickDistance = "ClickDistance";
        public const string CustomBlocks = "CustomBlocks";
        public const string HeldBlock = "HeldBlock";
        public const string TextHotkey = "TextHotKey";
        public const string ExtPlayerList = "ExtPlayerList";
        public const string EnvColors = "EnvColors";
        public const string SelectionCuboid = "SelectionCuboid";
        public const string BlockPermissions = "BlockPermissions";
        public const string ChangeModel = "ChangeModel";
        public const string EnvMapAppearance = "EnvMapAppearance";
        public const string EnvWeatherType = "EnvWeatherType";
        public const string HackControl = "HackControl";
        public const string EmoteFix = "EmoteFix";
        public const string MessageTypes = "MessageTypes";
        public const string LongerMessages = "LongerMessages";
        public const string FullCP437 = "FullCP437";
        public const string BlockDefinitions = "BlockDefinitions";
        public const string BlockDefinitionsExt = "BlockDefinitionsExt";
        public const string TextColors = "TextColors";
        public const string BulkBlockUpdate = "BulkBlockUpdate";
        public const string EnvMapAspect = "EnvMapAspect";
        public const string PlayerClick = "PlayerClick";
        public const string EntityProperty = "EntityProperty";
        public const string ExtEntityPositions = "ExtEntityPositions";
        public const string TwoWayPing = "TwoWayPing";
        public const string InventoryOrder = "InventoryOrder";
        public const string InstantMOTD = "InstantMOTD";
        public const string FastMap = "FastMap";
        public const string ExtBlocks = "ExtendedBlocks";
        public const string ExtTextures = "ExtendedTextures";
        public const string SetHotbar = "SetHotbar";
        public const string SetSpawnpoint = "SetSpawnpoint";
        public const string VelocityControl = "VelocityControl";
        public const string CustomParticles = "CustomParticles";
        public const string CustomModels = "CustomModels";
        public const string PluginMessages = "PluginMessages";
    }
    public partial class Player
    {
        public bool hasCpe;

        internal CpeExt FindExtension(string extName)
        {
            System.Collections.IList list = extensions;
            for (int i = 0; i < list.Count; i++)
            {
                CpeExt ext = (CpeExt)list[i];
                if (ext.Name.CaselessEq(extName)) return ext;
            }
            return null;
        }

        // these are checked very frequently, so avoid overhead of .Supports(
        public bool hasCustomBlocks, hasBlockDefs, hasTextColors, hasExtBlocks,
        hasChangeModel, hasExtList, hasCP437, hasBulkBlockUpdate;

        /// <summary> Whether this player's client supports the given CPE extension at the given version </summary>
        public bool Supports(string extName, int version = 1)
        {
            if (!hasCpe) return true;
            CpeExt ext = FindExtension(extName);
            return ext != null && ext.ClientVersion == version;
        }
    }
}