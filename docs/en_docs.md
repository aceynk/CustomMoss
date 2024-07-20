# CustomMoss Documentation

## Table of Contents

<!-- TOC -->
* [CustomMoss Documentation](#custommoss-documentation)
  * [Table of Contents](#table-of-contents)
  * [Info](#info)
  * [Trees](#trees)
    * [Tree Ids](#tree-ids)
  * [Stones](#stones)
    * [Rock Ids](#rock-ids)
<!-- TOC -->

## Info

CustomMoss aims to allow content packs to create custom moss variants for wild trees (and rocks!) in Stardew Valley.

## Trees

New moss variants can be made to grown on trees by targeting the asset ``"aceynk.CustomMoss/Tree"`` with CP.

This asset is a dictionary where the key is the qualified item id of the moss that should be growing on the tree,
and the value is a model matching the following:

| Field                     | Type   | Description                                                                                                                    |
|---------------------------|--------|--------------------------------------------------------------------------------------------------------------------------------|
| GrowConditions            | string | A GSQ referencing the valid state for this moss to grow in.<br/>See: https://stardewvalleywiki.com/Modding:Game_state_queries. |
| ValidTrees                | string | A list of tree ids, separated with commas. See [Tree Ids](#tree-ids).                                                          |
| MaxAmount                 | int    | The maximum amount of items to drop upon harvest.                                                                              |
| MinAmount                 | int    | The minimum amount of items to drop upon harvest.                                                                              |
| Chance                    | float  | The chance, per day, of this moss growing on a tree. Between 0 and 1.                                                          |
| Experience                | int    | How much foraging experience, per moss item, to award upon harvest.                                                            |
| TextureOak                | string | The texture asset for oak trees that have this moss.                                                                           |
| TextureMaple              | string | The texture asset for maple trees that have this moss.                                                                         |
| TexturePine               | string | The texture asset for pine trees that have this moss.                                                                          |
| Texture1                  | string | The texture asset for green rain type 1 trees that have this moss.                                                             |
| Texture2                  | string | The texture asset for green rain type 2 trees that have this moss.                                                             |
| Texture<CustomWildTreeId> | string | The texture asset for instances of the custom wild tree that have this moss.                                                   |

If textures are not given for the wild tree moss, it defaults to the vanilla moss texture.

I recommend using CP's ``{{Season}}`` token to change the texture of trees across seasons.

### Tree Ids

Valid tree ids are:

| Tree                                               | Id                                        |
|----------------------------------------------------|-------------------------------------------|
| Oak                                                | "1"                                       |
| Maple                                              | "2"                                       |
| Pine                                               | "3"                                       |
| GreenRainTreeType1 (Bushy)                         | "10"                                      |
| GreenRainTreeType2 (Leafy)                         | "11"                                      |
| Other trees (untested)<br/>Custom trees (untested) | Their respective TreeType ids as strings. |


## Stones

New moss variants can be made to grown on rocks by targeting the asset ``"aceynk.CustomMoss/Stone"`` with CP.

This asset is a dictionary where the key is the qualified item id of the moss that should be growing on the rock,
and the value is a model matching the following:

| Field          | Type   | Description                                                                                                                    |
|----------------|--------|--------------------------------------------------------------------------------------------------------------------------------|
| GrowConditions | string | A GSQ referencing the valid state for this moss to grow in.<br/>See: https://stardewvalleywiki.com/Modding:Game_state_queries. |
| MaxAmount      | int    | The maximum amount of items to drop upon harvest.                                                                              |
| MinAmount      | int    | The minimum amount of items to drop upon harvest.                                                                              |
| Chance         | float  | The chance, per day, of this moss growing on a rock. Between 0 and 1.                                                          |
| Experience     | int    | How much foraging experience, per moss item, to award upon harvest.                                                            |
| Texture        | string | The texture asset for rocks with this moss.                                                                                    |
| SpriteIndex343 | int    | The index of the texture for rocks with id 343 in the given texture asset.                                                     |
| SpriteIndex450 | int    | The index of the texture for rocks with id 450 in the given texture asset.                                                     |

### Rock Ids

Valid rock ids are:

| Rock                              | Id    |
|-----------------------------------|-------|
| Basic rock (with smaller pebbles) | "343" |
| Basic rock (larger, solid)        | "450" |