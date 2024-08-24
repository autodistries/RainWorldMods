using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Serialization.Configuration;
using UnityEngine;
namespace SleepySlugcat;

public class Zs : CosmeticSprite
{
    public int parentPlayerId = 0;
    public string rainbow = "";
    public int age;
    public float growthSpeed;
    public float size;
    public Color color;
    public float rotation;
    int facingTowards; // direction slugcat is pointing towards
    private Color originalColor; // used for individual rainbows
    public static bool decayEnabled = false; // global state

    private bool decaying = false; // state of this Z
    static float timecounter = 0f;
    public static string text = ":3";
    static int lastsecond; // try not to get color jumps on pause/resume
    public static float baseSizeVar = 0.35f;

    public static bool onlyZs = false;
    public static bool musician = false;
    Vector2 HQdxy = new();

    // based on public class LizardBubble : CosmeticSprite
    public Zs(Vector2 pos, Vector2 vel, int facing, Color col)
    {

        //text = text.Substring(0, ((int)(Random.value*10)%3+1));
        size = Mathf.Lerp(0.25f + baseSizeVar, 0.95f + baseSizeVar, UnityEngine.Random.value);
        facingTowards = facing;
        if (facingTowards == -1) pos.x -= size * 4f * text.Length;
        base.lastPos = pos;
        base.pos = pos;
        base.vel = vel;
        this.color = col;
        rotation = (System.Math.Abs(vel.x) * 10f) * facing * 30f;

    }


    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        base.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("ForegroundLights"));
    }
    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        //can stay empty prob
    }
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
            if (onlyZs) { HQdxy = new();}

        for (int i = 0; i < text.Length; i++)
        {
            float dx = System.Math.Abs(lastPos.x);
            sLeaser.sprites[i].rotation = (text.Length != 1 || !onlyZs) ? 0f : rotation;
            sLeaser.sprites[i].scale = size * 0.45f * (onlyZs && text[i]==122 ? 0.75f  : 1.0f);
            sLeaser.sprites[i].x = Mathf.Lerp(lastPos.x, pos.x, timeStacker) - camPos.x + i * sLeaser.sprites[i].scale * (13.5f ) + HQdxy.x;
            dx -= sLeaser.sprites[i].x;
            sLeaser.sprites[i].y = Mathf.Lerp(lastPos.y, pos.y, timeStacker) - camPos.y - facingTowards * i * 1.24f * (float)System.Math.Cos(rotation) * sLeaser.sprites[i].scale;
            if (onlyZs && !musician) {
                if (text[i] == 90 ) /* Z */ {
                    if (i!=text.Length-1 && text[i+1] == 122) {
                        HQdxy.x += sLeaser.sprites[i].scale *7.0f;
                    }
                    if (i!=0 && text[i-1] == 122) {
                        HQdxy.x -= sLeaser.sprites[i].scale *7.0f * (1f+HQdxy.y);
                    }
                    HQdxy.y = 0f;
                } else HQdxy.y+=1.0f;
                
            }
        }



        if (rainbow == "individual")
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (originalColor == default)
                {
                    //  Debug.Log("Setting originalcolor");
                    originalColor = new(color.r, color.g, color.b, color.a);
                }
                if (lastsecond != System.DateTime.Now.Second) { lastsecond = System.DateTime.Now.Second; timecounter += 1; timecounter %= 60; }

                color.g = (float)(System.Math.Sin(RWCustom.Custom.LerpMap(((lastsecond + i) % 60) + (System.DateTime.Now.Millisecond) / 1000f, 0f, 60f, 0f, (float)(10 * System.Math.PI))) / 2f + 0.5f);
                color.b = (float)(System.Math.Sin(RWCustom.Custom.LerpMap(((lastsecond + i) % 60) + (System.DateTime.Now.Millisecond) / 1000f, 0f, 60f, 0f, (float)(12 * System.Math.PI))) / 2f + 0.5f);
                color.r = (float)(System.Math.Sin(RWCustom.Custom.LerpMap(((lastsecond + i) % 60) + (System.DateTime.Now.Millisecond) / 1000f, 0f, 60f, 0f, (float)(18 * System.Math.PI))) / 2f + 0.5f);

                sLeaser.sprites[i].color = color;
            }
        }
        else if (rainbow == "unified")
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (lastsecond != System.DateTime.Now.Second) { lastsecond = System.DateTime.Now.Second; timecounter += 1; timecounter %= 60; }
                color.g = (float)(System.Math.Sin(RWCustom.Custom.LerpMap(timecounter + System.DateTime.Now.Millisecond / 1000f, 0f, 60f, 0f, (float)(10 * System.Math.PI))) / 2f + 0.5f);
                color.b = (float)(System.Math.Sin(RWCustom.Custom.LerpMap(timecounter + System.DateTime.Now.Millisecond / 1000f, 0f, 60f, 0f, (float)(12 * System.Math.PI))) / 2f + 0.5f);
                color.r = (float)(System.Math.Sin(RWCustom.Custom.LerpMap(timecounter + System.DateTime.Now.Millisecond / 1000f, 0f, 60f, 0f, (float)(18 * System.Math.PI))) / 2f + 0.5f);

                sLeaser.sprites[i].color = color;
            }

        }
        if (decayEnabled && decaying)
        {
            color.a -= 1f / 170f;
            for (int i = 0; i < text.Length; i++)
            {

                sLeaser.sprites[i].color = color;
            }
            if (color.a < 0.005)
            {
                //  Debug.Log("decay end- destroy! "+age);
                Destroy();
            }
        }


        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

    }
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[(!musician) ? text.Length : 1];



        if (musician) {
            string targetSilence;
            if (UnityEngine.Random.Range(0f, 1f) > 0.965) {
                targetSilence = "short";
            } else targetSilence = "regular";
            if (!Futile.atlasManager.DoesContainElementWithName("silence-" + targetSilence))
                { //this loads the image to atlas !
                    string targetPath = Path.Combine(Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.FullName, "images", "silence-" + targetSilence);//UnityEngine.Application.streamingAssetsPath+"/mods/SleepySlugcat/Zs";
                    Futile.atlasManager.ActuallyLoadAtlasOrImage("silence-" + targetSilence, targetPath, "");
                }

                sLeaser.sprites[0] = new FSprite("silence-" + targetSilence);
                sLeaser.sprites[0].color = color;
                sLeaser.sprites[0].scale = 0.92f;
        } else
        if (!onlyZs)
        {

            for (int i = 0; i < text.Length; i++)
            {
                string c = text[i].ToString();
                c = translateSymbols(c);
                if (!Futile.atlasManager.DoesContainElementWithName("lettre-" + c))
                { //this loads the image to atlas !
                    string targetPath = Path.Combine(Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.FullName, "images", "lettre-" + c);//UnityEngine.Application.streamingAssetsPath+"/mods/SleepySlugcat/Zs";
                    Futile.atlasManager.ActuallyLoadAtlasOrImage("lettre-" + c, targetPath, "");
                }

                sLeaser.sprites[i] = new FSprite("lettre-" + c);
                sLeaser.sprites[i].color = color;

            }
        }
         else {
            for (int i = 0; i < text.Length; i++)
            {

                if (!Futile.atlasManager.DoesContainElementWithName("Zs"))
                { //this loads the image to atlas !
                    string targetPath = Path.Combine(Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.FullName, "Zs");//UnityEngine.Application.streamingAssetsPath+"/mods/SleepySlugcat/Zs";
                    Futile.atlasManager.ActuallyLoadAtlasOrImage("Zs", targetPath, "");
                }

                sLeaser.sprites[i] = new FSprite("Zs");
                sLeaser.sprites[i].color = color;

            }
        }


        AddToContainer(sLeaser, rCam, null);

    }

    private string translateSymbols(string c)
    {
        if ((c[0] >= 65/*A*/ && c[0] <= 90 /*Z*/)) return c; //uppercase only. Damn you, windows
        if ((c[0] >= 97 && c[0] <= 122)) return c + "tiny";

        return c[0] switch
        {
            '&' => "ampersand",
            '^' => "asciicircum",
            '~' => "asciitilde",
            '*' => "asterisk",
            '@' => "at",
            '\\' => "backslash",
            '|' => "bar",
            '{' => "braceleft",
            '}' => "braceright",
            '[' => "bracketleft",
            ']' => "bracketright",
            ':' => "colon",
            ',' => "comma",
            '$' => "dollar",
            '!' => "exclam",
            '=' => "equal",
            '>' => "greater",
            '<' => "less",
            '-' => "hyphen",
            '#' => "numbersign",
            '%' => "percent",
            '.' => "period",
            '+' => "plus",
            '?' => "question",
            '"' => "quotedbl",
            '`' => "quoteleft",
            '\'' => "quoteright",
            ';' => "semicolon",
            '/' => "slash",
            ' ' => "space",
            '_' => "underscore",
            '0' => "zero",
            '1' => "one",
            '2' => "two",
            '3' => "three",
            '4' => "four",
            '5' => "five",
            '6' => "six",
            '7' => "seven",
            '8' => "eight",
            '9' => "nine",
            '(' => "parenleft",
            ')' => "parenright",
            _ => "question",
        };
        ;
    }

    public override void Update(bool eu)
    {
        if (Random.value < ((System.Math.Abs(vel.x) > 0.7f) ? 0.026 : 0.007))
        {
            vel.x /= (1 + Random.value);
        }
        age++;
        rotation = (System.Math.Abs(vel.x) * 10f) * facingTowards * 30f;

        if ((age > (decayEnabled ? 300 : 400) && UnityEngine.Random.value < 0.02f * (1f + (float)age / 60000f)) || age > 700)
        {
            if (!decayEnabled) Destroy();
            else decaying = true;

        }


        base.Update(eu);
    }



}