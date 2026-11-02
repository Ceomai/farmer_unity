// ================= FOTOĞRAF MODU (31-photo.js) =================
// Dünya kamerasından ekran görüntüsü alınır (arayüz hariç), çerçeveli kartpostal olarak birleştirilir ve gösterilir.
// Kaydetme: görsele uzun bas → JPG Application.persistentDataPath altına yazılır.
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Ciftlik
{
    public class Photo_Owner : MonoBehaviour { public Texture tex; void OnDestroy() { if (tex) Destroy(tex); } }
    public class Photo_RightClick : MonoBehaviour, IPointerClickHandler { public Action action; public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Right) action?.Invoke(); } }

    public static partial class Game
    {
        public static void PhotoMode(bool on)
        {
            Int_BodyToggle("photo", on);
            if (on) { View = null; RenderSheet(); Toast("Kadrajı ayarla, deklanşöre bas"); }
        }

        static Texture2D Photo_Render()
        {
            int w = Mathf.Max(16, Screen.width), h = Mathf.Max(16, Screen.height);
            var rt = new RenderTexture(w, h, 24); rt.antiAliasing = 4; rt.Create();
            var prevT = Cam.targetTexture; bool en = Cam.enabled;
            Cam.targetTexture = rt; Cam.Render(); Cam.targetTexture = prevT; Cam.enabled = en;
            var prevA = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = prevA; rt.Release(); UnityEngine.Object.Destroy(rt);
            return tex;
        }
        // <canvas> birleştirmesi: kâğıt zemin, fotoğraf, çerçeve ve yazılar (ekran dışı uGUI → RenderTexture)
        static Texture2D Photo_Compose(Texture2D im)
        {
            int W = 1080, ih = RoundI(W * (double)im.height / im.width), pad = 40, cw = W + pad * 2, chh = ih + pad * 2 + 120;
            var rt = new RenderTexture(cw, chh, 24); rt.Create();
            var cgo = new GameObject("photoCam"); var c = cgo.AddComponent<Camera>(); c.enabled = false; c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = UI.Hex("#fbf6ea");
            c.cullingMask = 1 << 5; c.orthographic = true; c.nearClipPlane = .1f; c.farClipPlane = 10; c.targetTexture = rt;
            cgo.transform.position = new Vector3(0, -5000, 0);
            var kgo = new GameObject("photoCanvas"); kgo.layer = 5; var cv = kgo.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceCamera; cv.worldCamera = c; cv.planeDistance = 1;
            var root = (RectTransform)kgo.transform;
            RectTransform at(Component comp, float x, float y, float ww, float hh) { var r = (RectTransform)comp.transform; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(ww, hh); return r; }
            var img = UI.New("img", root).gameObject.AddComponent<RawImage>(); img.texture = im; at(img, pad, pad, W, ih);
            var ink = UI.Hex("#2a1a10");
            at(UI.Fill(root, ink), pad - 2, pad - 2, W + 4, 4); at(UI.Fill(root, ink), pad - 2, pad + ih - 2, W + 4, 4);
            at(UI.Fill(root, ink), pad - 2, pad - 2, 4, ih + 4); at(UI.Fill(root, ink), pad + W - 2, pad - 2, 4, ih + 4);
            var t1 = UI.Txt(root, "Çiftlik İmparatorluğu", 44, UI.Hex("#3a2414"), false, TextAnchor.LowerCenter, false);
            Font geo = null; try { geo = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Noto Serif", "serif" }, 44); } catch (Exception) { }
            if (geo != null) t1.font = geo; t1.fontStyle = FontStyle.Italic; at(t1, 0, ih + pad + 62 - 56, cw, 60);
            var t2 = UI.Txt(root, SEASONS[Season()].n + " · " + (GameDay() % 30 + 1) + ". gün · Seviye " + S.lv + " · " + HOUSE[S.house - 1].n, 24, UI.C.muted, true, TextAnchor.LowerCenter, false);
            at(t2, 0, ih + pad + 100 - 32, cw, 36);
            W_SetLayer(root, 5);
            Canvas.ForceUpdateCanvases(); c.Render();
            var prevA = RenderTexture.active; RenderTexture.active = rt;
            var outT = new Texture2D(cw, chh, TextureFormat.RGB24, false); outT.ReadPixels(new Rect(0, 0, cw, chh), 0, 0); outT.Apply();
            RenderTexture.active = prevA;
            c.targetTexture = null; UnityEngine.Object.Destroy(kgo); UnityEngine.Object.Destroy(cgo); rt.Release(); UnityEngine.Object.Destroy(rt);
            return outT;
        }
        static void Photo_Save(Texture2D t)
        {
            try
            {
                var name = "ciftlik-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jpg";
                var path = Path.Combine(Application.persistentDataPath, name);
                File.WriteAllBytes(path, t.EncodeToJPG(90));
                Toast("📸 " + name);
            }
            catch (Exception e) { Debug.LogException(e); }
        }
        public static void Shoot()
        {
            if (Cam == null) { Toast("Görüntü hazırlanıyor, birazdan tekrar dene"); return; }
            var fl = UI.Fill(UI.Overlay, Color.white, 0, "flash"); UI.Stretch(fl.rectTransform); fl.raycastTarget = false; fl.transform.SetAsLastSibling();
            UI.Tween(.45f, k => { if (fl) fl.color = new Color(1, 1, 1, 1 - k); }); SetTimeout(() => UI.Kill(fl), 500);
            Tone(1800, .05, "square", .05); Tone(900, .08, "square", .04, .06);
            Texture2D shot = Photo_Render(); Texture2D card;
            try { card = Photo_Compose(shot); } finally { UnityEngine.Object.Destroy(shot); }
            Stat("photo");
            UI.Modal(box =>
            {
                box.gameObject.AddComponent<Photo_Owner>().tex = card;
                float mw = Mathf.Min(560, UI.W * .94f) - 36;
                var ti = UI.Title(box, "📸 Çiftlik fotoğrafı", 24); ti.alignment = TextAnchor.MiddleCenter;
                var pic = UI.Picture(box, card, mw, mw * card.height / card.width); pic.raycastTarget = true;
                var sh = pic.gameObject.AddComponent<Shadow>(); sh.effectColor = UI.C.ink; sh.effectDistance = new Vector2(3, -4);
                UI.OnLongPress(pic, () => Photo_Save(card)); pic.gameObject.AddComponent<Photo_RightClick>().action = () => Photo_Save(card);
                var note = UI.Txt(box, "Kaydetmek için görsele uzun bas (telefon) ya da sağ tıklayıp \"Resmi kaydet\"i seç.", 12.5f, UI.C.muted, false, TextAnchor.MiddleCenter);
                var row = UI.HStack(box, 8, TextAnchor.MiddleCenter, null, "urow");
                UI.Btn(row, "📷 Bir tane daha", () => UI.Kill(box.parent), "gold");
                UI.Btn(row, "Bitti", () => { UI.Kill(box.parent); PhotoMode(false); }, "soft");
            }, true, false, Mathf.Min(560, UI.W * .94f));
        }
    }
}
