using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using UndertaleModLib.Util;

UndertaleEmbeddedTexture embed = null;
EnsureDataLoaded();

string imgPath = PromptLoadFile(".png", "MagicaVoxel Vox2Png Output File|*.png|All Files|*");
if(imgPath == null) return;

string dimensions = ScriptInputDialog(ScriptPath, "Type MagicaVoxel model dimensions:", "0 0 0", "Cancel", "Import", true, false);
if(dimensions == null) return;

string spriteName = ScriptInputDialog(ScriptPath, "Type Sprite name (ex. sprHandgun, sprMumbaLauncher):", "sprWeapon" + Data.Sprites.Count, "Cancel", "Continue", true, false);
if(spriteName == null) return;

for (int i = 0 ; i < Data.Sprites.Count ; i++)
{
	var sprite = Data.Sprites[i];
	if(sprite?.Name?.Content == spriteName)
	{
		ScriptError("Cannot overwrite " + spriteName + ", use another name instead!");
		return;
	}
}

int originalWidth = 0;
int originalHeight = 0;
cloneEmbeddedTexture();
string[] dimArray = dimensions.Split(" ");

if(dimArray.Length < 3)
{
	ScriptError("Invalid Dimensions! Check out the dimension through MagicaVoxel, copy and paste it.");
	return;
}

int columns = int.Parse(dimArray[0]);
int rows = int.Parse(dimArray[1]);
int layers = int.Parse(dimArray[2]);

if(layers < 1)
{
	ScriptError("Invalid height for this model, stopping script.");
	return;
}

UndertaleSprite spriteCreated = makeSprites();

string gameObjectName = ScriptInputDialog(ScriptPath, "Type Game Object name (ex. oFlameMinigun):", "oWeapon" + Data.GameObjects.Count, "Cancel", "Continue", true, false);
if(gameObjectName != null && gameObjectName != "gun") //"gun" Failsafe
{
	makeWeaponGameObject(spriteCreated);
}
else ChangeSelection(spriteCreated, true);

ScriptMessage("Script finished");



void makeWeaponGameObject(UndertaleSprite spriteCreated)
{
	UndertaleGameObject obj = Data.GameObjects.ByName(gameObjectName);
	if(obj == null)
	{
		obj = new UndertaleGameObject();
		obj.Name = Data.Strings.MakeString(gameObjectName);
		obj.Sprite = spriteCreated;
		obj.ParentId = Data.GameObjects.ByName("gun");
		Data.GameObjects.Add(obj);
	}
	obj.Persistent = true;
	obj.CollisionShape = CollisionShapeFlags.Box;
	obj.Group = 1;
	obj.Awake = true;
	ChangeSelection(obj, true);
}

UndertaleSprite makeSprites()
{
	UndertaleSprite newSprite = Data.Sprites.ByName(spriteName);
	if(newSprite == null)
	{
		newSprite = new UndertaleSprite();
		newSprite.Name = Data.Strings.MakeString(spriteName);
		Data.Sprites.Add(newSprite);
	}
	newSprite.Width = (uint) columns;
	newSprite.Height = (uint) rows;
	newSprite.MarginLeft = 0;
	newSprite.MarginRight = columns-1;
	newSprite.MarginTop = 0;
	newSprite.MarginBottom = rows-1;
	newSprite.OriginX = (int) Math.Floor((double) columns / 2);
	newSprite.OriginY = (int) Math.Floor((double) rows / 2);
	newSprite.BBoxMode = (uint) 2;
	newSprite.IsSpecialType = true;
	newSprite.SVersion = (uint) 3;
	newSprite.GMS2PlaybackSpeed = 30.0f;

	for (int i = 0 ; i < layers ; i++)
	{
		UndertaleTexturePageItem newItem = new UndertaleTexturePageItem();
		newItem.Name = new UndertaleString("PageItem " + Data.TexturePageItems.Count);
		newItem.SourceX = (ushort) Math.Floor((double) (i * columns) % originalWidth);
		newItem.SourceY = (ushort) (Math.Floor((double) (i * columns) / originalWidth) * rows);
		newItem.SourceWidth = (ushort) columns;
		newItem.SourceHeight = (ushort) rows;
		newItem.TargetX = 0;
		newItem.TargetY = 0;
		newItem.TargetWidth = (ushort) columns;
		newItem.TargetHeight = (ushort) rows;
		newItem.BoundingWidth = (ushort) columns;
		newItem.BoundingHeight = (ushort) rows;
		newItem.TexturePage = embed;
		Data.TexturePageItems.Add(newItem);
		
		UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
		texentry.Texture = newItem;
		newSprite.Textures.Add(texentry);
	}
	return newSprite;
}

void cloneEmbeddedTexture()
{
	try
	{
		Bitmap bitmap = new Bitmap(imgPath);
		bitmap.SetResolution(96.0F, 96.0F);

		originalWidth = bitmap.Width;
		originalHeight = bitmap.Height;
		Image img = new Bitmap(closestPower(originalWidth), closestPower(originalHeight));
		Graphics g = Graphics.FromImage(img);

		g.DrawImage(bitmap, 0, 0);
		bitmap = new Bitmap(img);

		embed = new UndertaleEmbeddedTexture();
		embed.Name = new UndertaleString("Texture " + Data.EmbeddedTextures.Count);
		embed.Scaled = 1;

		ImageConverter converter = new ImageConverter();
		embed.TextureData.TextureBlob = (byte[]) converter.ConvertTo(bitmap, typeof(byte[]));
		Data.EmbeddedTextures.Add(embed);
	}
	catch(Exception ex)
	{
		//embed failure!! Laugh at this user
		ScriptMessage("Failed to import file \"" + imgPath + "\" due to: " + ex.Message);
	}
}

int closestPower(double val)
{
	return (int) Math.Pow(2, Math.Ceiling(Math.Log(val) / Math.Log(2)));
}
