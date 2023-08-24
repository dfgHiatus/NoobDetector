using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Linq;
using static ModNameGoesHere.NoobDetector;

namespace ModNameGoesHere
{
    internal class NoobDetectorJoinPanel : Component
    {
        protected readonly Sync<Uri> URL;

        protected readonly SyncRef<Text> _hyperlinkText;

        protected readonly SyncRef<Text> _reasonText;

        protected readonly SyncRef<Button> _openButton;
        private UserDescriptor userDescriptor; 

        public override bool UserspaceOnly => true;

        public void Setup(UserDescriptor userDesc)
        {
            userDescriptor = userDesc;
            var uri = new Uri(userDesc.SessionURLs.First());
            URL.Value = uri;
            _hyperlinkText.Target.Content.Value = uri.ToString();
            _reasonText.Target.Content.SetLocalized($"A new user {userDesc.Username} was born into the universe!\nGo say hi to them!");
        }

        protected override void OnAttach()
        {
            base.OnAttach();
            NeosCanvasPanel neosCanvasPanel = Slot.AttachComponent<NeosCanvasPanel>();
            neosCanvasPanel.Panel.AddCloseButton();
            neosCanvasPanel.Panel.Title = "A new user was born!";
            neosCanvasPanel.Panel.Thickness.Value *= 0.5f;
            neosCanvasPanel.CanvasSize = new float2(300f, 200f);
            neosCanvasPanel.PhysicalHeight = 0.1f;
            UIBuilder uIBuilder = new UIBuilder(neosCanvasPanel.Canvas);
            uIBuilder.VerticalLayout(4f);
            uIBuilder.Style.MinHeight = 64f;
            LocaleString text = $"A new user {userDescriptor.Username} was born into the universe!\nGo say hi to them!";
            uIBuilder.Text(in text);
            uIBuilder.Style.MinHeight = 32f;
            SyncRef<Text> hyperlinkText = _hyperlinkText;
            text = "---";
            hyperlinkText.Target = uIBuilder.Text(in text);
            _hyperlinkText.Target.Color.Value = new color(0f, 1f, 1f);
            uIBuilder.Style.MinHeight = 24f;
            SyncRef<Text> reasonText = _reasonText;
            text = "---";
            reasonText.Target = uIBuilder.Text(in text);
            uIBuilder.Style.MinHeight = 32f;
            uIBuilder.HorizontalLayout(4f);
            text = "Go to the user";
            color a = color.Green;
            color b = color.White;
            color tint = MathX.Lerp(in a, in b, 0.5f);
            Button target = uIBuilder.Button(in text, in tint, Open);
            text = "Cancel";
            a = color.Red;
            b = color.White;
            tint = MathX.Lerp(in a, in b, 0.5f);
            uIBuilder.Button(in text, in tint, Cancel);
            _openButton.Target = target;
            RunInUpdates(2, delegate
            {
                Slot temp = World.AddSlot("TEMP");
                temp.GlobalPosition = float3.Up;
                Slot prevParent = Slot.Parent;
                Slot.Parent = temp;
                RunInUpdates(2, delegate
                {
                    Slot.Parent = prevParent;
                    temp.Destroy();
                });
            });
        }

        [SyncMethod]
        private void Cancel(IButton button, ButtonEventData eventData)
        {
            if (World == Userspace.UserspaceWorld)
            {
                Slot.Destroy();
            }
        }

        [SyncMethod]
        private void Open(IButton button, ButtonEventData eventData)
        {
            if (World != Userspace.UserspaceWorld)
            {
                return;
            }
            if (URL.Value != null)
            {
                RunInBackground(delegate
                {
                    Engine.Current.WorldManager.JoinSession(new Uri[] { URL.Value });
                });
            }
            Slot.Destroy();
        }
    }
}
