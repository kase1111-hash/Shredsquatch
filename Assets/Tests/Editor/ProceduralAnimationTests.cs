using NUnit.Framework;
using UnityEngine;
using Shredsquatch.Player;
using Shredsquatch.Procedural;
using Shredsquatch.Sasquatch;
using Shredsquatch.Tricks;

namespace Shredsquatch.Tests.Editor
{
    /// <summary>
    /// The procedural animation math: limb IK, the rider's poses for movement and tricks,
    /// and the Sasquatch's gait.
    /// </summary>
    [TestFixture]
    public class ProceduralAnimationTests
    {
        private const float Dt = 1f / 60f;
        private const float BoardHeight = 0.05f;

        private static readonly RiderPoseInput Riding = new RiderPoseInput { Grounded = true, SpeedKmh = 50f };
        private static readonly RiderPoseInput Airborne = new RiderPoseInput { Grounded = false, SpeedKmh = 60f };

        private RiderPoseSolver _rider;

        [SetUp]
        public void SetUp()
        {
            _rider = new RiderPoseSolver(1.6f, BoardHeight);
        }

        private static RiderPose Run(RiderPoseSolver solver, RiderPoseInput input, float seconds)
        {
            RiderPose pose = solver.Pose;
            for (float t = 0f; t < seconds; t += Dt)
            {
                pose = solver.Step(input, Dt);
            }
            return pose;
        }

        // Rig-space point relative to the player root (the feet)
        private static Vector3 ToRoot(RiderPose pose, Vector3 rigPoint)
        {
            return pose.RigPosition + pose.RigRotation * rigPoint;
        }

        private static void AssertLegsOnBoard(RiderPose pose)
        {
            Assert.AreEqual(RiderPoseSolver.ThighLength, Vector3.Distance(pose.HipLeft, pose.KneeLeft), 0.001f);
            Assert.AreEqual(RiderPoseSolver.ShinLength, Vector3.Distance(pose.KneeRight, pose.AnkleRight), 0.001f);

            Vector3 frontBinding = pose.BoardPosition + pose.BoardRotation
                * new Vector3(0f, RiderPoseSolver.AnkleHeight, RiderPoseSolver.StanceHalfLength);
            Vector3 backBinding = pose.BoardPosition + pose.BoardRotation
                * new Vector3(0f, RiderPoseSolver.AnkleHeight, -RiderPoseSolver.StanceHalfLength);
            Assert.Less(Vector3.Distance(pose.AnkleLeft, frontBinding), 0.01f, "front foot came off the board");
            Assert.Less(Vector3.Distance(pose.AnkleRight, backBinding), 0.01f, "back foot came off the board");
        }

        [Test]
        public void TwoBoneIK_KeepsSegmentLengthsAndReachesTarget()
        {
            Vector3 root = new Vector3(0.1f, 1f, 0f);
            Vector3 target = new Vector3(0.3f, 0.4f, 0.4f);

            Vector3 joint = ProceduralAnimation.SolveTwoBone(root, target, 0.42f, 0.42f, Vector3.forward, out Vector3 end);

            Assert.AreEqual(0.42f, Vector3.Distance(root, joint), 0.0001f);
            Assert.AreEqual(0.42f, Vector3.Distance(joint, end), 0.0001f);
            Assert.Less(Vector3.Distance(end, target), 0.0001f);
        }

        [Test]
        public void TwoBoneIK_PointsStraightAtUnreachableTarget()
        {
            Vector3 joint = ProceduralAnimation.SolveTwoBone(Vector3.zero, new Vector3(0f, 0f, 5f), 1f, 1f,
                Vector3.up, out Vector3 end);

            Assert.AreEqual(2f, end.z, 0.01f);
            Assert.AreEqual(1f, joint.z, 0.05f);
            Assert.AreEqual(0f, end.y, 0.0001f);
        }

        [Test]
        public void TwoBoneIK_BendsTowardHint()
        {
            Vector3 joint = ProceduralAnimation.SolveTwoBone(Vector3.zero, new Vector3(0f, -1f, 0f), 0.7f, 0.7f,
                Vector3.forward, out _);

            Assert.Greater(joint.z, 0.3f, "knee should point the way of the hint");
        }

        [Test]
        public void Riding_KeepsFeetOnBoardAndBoardOnSnow()
        {
            var carve = Riding;
            carve.Lean = 40f;
            carve.Steer = 1f;
            var tuck = Riding;
            tuck.Tucking = true;
            var brake = Riding;
            brake.Braking = true;
            var charge = Riding;
            charge.Charging = true;
            charge.Charge = 1f;

            foreach (var state in new[] { Riding, carve, tuck, brake, charge })
            {
                RiderPose pose = Run(_rider, state, 1f);
                AssertLegsOnBoard(pose);
                Assert.AreEqual(BoardHeight, ToRoot(pose, pose.BoardPosition).y, 0.002f);
            }
        }

        [Test]
        public void TuckAndJumpCharge_LowerTheView()
        {
            float standing = ToRoot(Run(_rider, Riding, 1f), _rider.Pose.CameraPosition).y;

            var tuck = Riding;
            tuck.Tucking = true;
            RiderPose tucked = Run(_rider, tuck, 1f);
            Assert.Less(ToRoot(tucked, tucked.CameraPosition).y, standing - 0.1f);

            var charge = Riding;
            charge.Charging = true;
            charge.Charge = 1f;
            RiderPose charged = Run(_rider, charge, 1f);
            Assert.Less(ToRoot(charged, charged.CameraPosition).y, ToRoot(tucked, tucked.CameraPosition).y);
        }

        [Test]
        public void Carving_PutsTheBoardOnEdgeIntoTheTurn()
        {
            var carveRight = Riding;
            carveRight.Lean = 40f;
            RiderPose pose = Run(_rider, carveRight, 1f);

            Vector3 boardRight = pose.BoardRotation * Vector3.right;
            Assert.Less(boardRight.y, -0.3f, "right edge should dig in on a right turn");
        }

        [Test]
        public void Jump_LiftsTheNose()
        {
            Run(_rider, Riding, 1f);
            _rider.Jump();
            RiderPose pose = Run(_rider, Airborne, 0.05f);

            Vector3 nose = pose.BoardRotation * Vector3.forward;
            Assert.Greater(nose.y, 0.2f);
        }

        [Test]
        public void Landing_CompressesKneesThenRecovers()
        {
            float riding = Run(_rider, Riding, 1f).RigPosition.y;

            _rider.Land(1.5f);
            float lowest = riding;
            for (int frame = 0; frame < 30; frame++)
            {
                lowest = Mathf.Min(lowest, _rider.Step(Riding, Dt).RigPosition.y);
            }

            Assert.Less(lowest, riding - 0.1f, "a big landing should sink the hips");
            RiderPose settled = Run(_rider, Riding, 1f);
            Assert.AreEqual(riding, settled.RigPosition.y, 0.02f);
            Assert.AreEqual(BoardHeight, ToRoot(settled, settled.BoardPosition).y, 0.002f);
        }

        [Test]
        public void Landing_IgnoresGroundContactFlicker()
        {
            float riding = Run(_rider, Riding, 1f).RigPosition.y;
            _rider.Land(0.05f);
            Assert.AreEqual(riding, Run(_rider, Riding, 0.1f).RigPosition.y, 0.005f);
        }

        [Test]
        public void Flip_RotatesBodyWithFlipAngle()
        {
            var flip = Airborne;
            flip.Flipping = true;
            flip.FlipAngle = 180f;

            RiderPose pose = _rider.Step(flip, Dt);

            Assert.Less((pose.RigRotation * Vector3.up).y, -0.95f, "rider should be upside down half way round");
        }

        [Test]
        public void Flip_SettlesUprightAfterBotchedLanding()
        {
            var flip = Airborne;
            flip.Flipping = true;
            for (float angle = 0f; angle <= 200f; angle += 6f)
            {
                flip.FlipAngle = angle;
                _rider.Step(flip, Dt);
            }

            _rider.Land(1.2f);
            float lowestEyes = float.MaxValue;
            for (int frame = 0; frame < 60; frame++)
            {
                RiderPose pose = _rider.Step(Riding, Dt);
                lowestEyes = Mathf.Min(lowestEyes, ToRoot(pose, pose.CameraPosition).y);
            }

            Assert.Less(Quaternion.Angle(Quaternion.identity, _rider.Pose.RigRotation), 1f);
            Assert.Greater(lowestEyes, 0.15f, "view should not dip under the snow while rolling upright");
        }

        [TestCase(GrabType.Nose, false)]
        [TestCase(GrabType.Indy, true)]
        [TestCase(GrabType.Melon, false)]
        [TestCase(GrabType.Stalefish, true)]
        public void Grab_PutsTheHandOnTheBoard(GrabType grab, bool rightHand)
        {
            var grabbing = Airborne;
            grabbing.Grab = grab;
            RiderPose pose = Run(_rider, grabbing, 1f);

            Assert.IsTrue(pose.IsGrabbing);
            Vector3 hand = rightHand ? pose.HandRight : pose.HandLeft;
            Assert.Less(Vector3.Distance(hand, pose.GrabPoint), 0.03f);
            Assert.Greater(pose.BoardPosition.y, Run(new RiderPoseSolver(1.6f, BoardHeight), Airborne, 1f).BoardPosition.y,
                "knees should pull the board up");
            AssertLegsOnBoard(pose);
        }

        [Test]
        public void Grab_ReleasesWhenLetGo()
        {
            var grabbing = Airborne;
            grabbing.Grab = GrabType.Indy;
            Run(_rider, grabbing, 0.5f);

            RiderPose pose = Run(_rider, Airborne, 0.5f);

            Assert.IsFalse(pose.IsGrabbing);
            Assert.Greater(Vector3.Distance(pose.HandRight, pose.BoardPosition), 0.4f);
        }

        [Test]
        public void HeadingSnap_SwingsTheViewRoundInsteadOfTeleporting()
        {
            Run(_rider, Riding, 1f);

            _rider.CorrectHeading(-180f);
            Assert.Greater(Quaternion.Angle(Quaternion.identity, _rider.Step(Riding, Dt).RigRotation), 120f);

            Assert.Less(Quaternion.Angle(Quaternion.identity, Run(_rider, Riding, 1f).RigRotation), 1f);
        }

        [Test]
        public void HeadingSnap_IgnoresSmallSteeringClamps()
        {
            Run(_rider, Riding, 1f);
            _rider.CorrectHeading(-3f);
            Assert.Less(Quaternion.Angle(Quaternion.identity, _rider.Step(Riding, Dt).RigRotation), 0.01f);
        }

        [Test]
        public void Crash_TumblesOntoTheBackThenStandsUp()
        {
            Run(_rider, Riding, 1f);
            var crashed = Riding;
            crashed.Crashed = true;

            float lowestEyes = float.MaxValue;
            RiderPose pose = default;
            for (int frame = 0; frame < 150; frame++)
            {
                pose = _rider.Step(crashed, Dt);
                lowestEyes = Mathf.Min(lowestEyes, ToRoot(pose, pose.CameraPosition).y);
            }

            Assert.Greater((pose.RigRotation * Vector3.forward).y, 0.8f, "should end up looking at the sky");
            Assert.Less(ToRoot(pose, pose.CameraPosition).y, 0.8f, "should be lying in the snow");
            Assert.Greater(lowestEyes, 0.15f, "head should stay out of the snow");

            var recovering = Riding;
            recovering.Recovering = true;
            Run(_rider, recovering, 1.5f);
            pose = Run(_rider, Riding, 1f);

            Assert.Less(Quaternion.Angle(Quaternion.identity, pose.RigRotation), 1f);
            AssertLegsOnBoard(pose);
        }

        [Test]
        public void EdgeCatch_SlamsOntoTheCarvingSide()
        {
            var carveRight = Riding;
            carveRight.Lean = 44f;
            Run(_rider, carveRight, 1f);

            _rider.EdgeCatch();
            var crashed = Riding;
            crashed.Crashed = true;
            RiderPose pose = Run(_rider, crashed, 1f);

            Assert.Less((pose.RigRotation * Vector3.right).y, -0.8f, "should be lying on the right side");
        }

        [Test]
        public void Grind_LiftsTheBoardOntoTheRailSideways()
        {
            var grinding = Riding;
            grinding.Grinding = true;
            grinding.Balance = 0.5f;
            RiderPose pose = Run(_rider, grinding, 1f);

            Assert.Greater(ToRoot(pose, pose.BoardPosition).y, 0.35f);
            Assert.Greater(Mathf.Abs((pose.BoardRotation * Vector3.forward).x), 0.9f, "board should be across the rail");
            Assert.Greater(Quaternion.Angle(Quaternion.identity, pose.RigRotation), 2f, "body should lean with the wobble");
            AssertLegsOnBoard(pose);
        }

        [Test]
        public void BoxSlide_KeepsTheBoardOnTheDeck()
        {
            // Park boxes are ridden on top, so the player root is already on the deck
            var sliding = Riding;
            sliding.Grinding = true;
            sliding.OnBox = true;
            sliding.Balance = 0.5f;
            RiderPose pose = Run(_rider, sliding, 1f);

            // Within a couple of centimetres: the balance lean pivots the body at the hips
            Assert.AreEqual(BoardHeight, ToRoot(pose, pose.BoardPosition).y, 0.02f, "board should not lift off the deck");
            Assert.Greater(Mathf.Abs((pose.BoardRotation * Vector3.forward).x), 0.9f, "board should still be across the box");
            AssertLegsOnBoard(pose);
        }

        [Test]
        public void BoxOllieCharge_SinksIntoTheKnees()
        {
            var sliding = Riding;
            sliding.Grinding = true;
            sliding.OnBox = true;
            RiderPose slide = Run(_rider, sliding, 1f);

            var charging = sliding;
            charging.Charging = true;
            charging.Charge = 1f;
            RiderPose charged = Run(_rider, charging, 1f);

            Assert.Less(ToRoot(charged, charged.CameraPosition).y, ToRoot(slide, slide.CameraPosition).y - 0.1f);
            Assert.AreEqual(BoardHeight, ToRoot(charged, charged.BoardPosition).y, 0.002f);
            AssertLegsOnBoard(charged);
        }

        [Test]
        public void SasquatchGait_KeepsAFootPlantedWhileRunning()
        {
            var gait = new SasquatchGait();
            for (int frame = 0; frame < 600; frame++)
            {
                SasquatchPose pose = gait.Step(25f, false, Dt);
                if (frame < 120) continue;

                float lowerFoot = Mathf.Min(pose.FootLeft.y, pose.FootRight.y);
                Assert.AreEqual(SasquatchGait.AnkleHeight, lowerFoot, 0.01f);
                Assert.AreEqual(SasquatchGait.ThighLength, Vector3.Distance(pose.HipLeft, pose.KneeLeft), 0.001f);
                Assert.AreEqual(SasquatchGait.ForearmLength, Vector3.Distance(pose.ElbowRight, pose.HandRight), 0.001f);
            }
        }

        [Test]
        public void SasquatchGait_StridesLengthenWithSpeed()
        {
            Assert.Less(MaxStride(4f), MaxStride(14f));
            Assert.AreEqual(0f, MaxStride(0f), 0.01f);
        }

        private static float MaxStride(float speed)
        {
            var gait = new SasquatchGait();
            float stride = 0f;
            for (int frame = 0; frame < 600; frame++)
            {
                SasquatchPose pose = gait.Step(speed, false, Dt);
                if (frame > 300) stride = Mathf.Max(stride, Mathf.Abs(pose.FootLeft.z));
            }
            return stride;
        }

        [Test]
        public void SasquatchGait_RoarsAndLunges()
        {
            var gait = new SasquatchGait();
            float runningHand = gait.Step(20f, false, Dt).HandLeft.y;

            gait.Roar();
            float roarHand = 0f;
            for (int frame = 0; frame < 60; frame++) roarHand = Mathf.Max(roarHand, gait.Step(20f, false, Dt).HandLeft.y);
            Assert.Greater(roarHand, runningHand + 1.5f, "arms should go up to roar");

            gait.Lunge();
            float reach = 0f;
            for (int frame = 0; frame < 45; frame++) reach = Mathf.Max(reach, gait.Step(0f, false, Dt).HandRight.z);
            Assert.Greater(reach, 1.2f, "arms should reach out for the rider");
        }
    }
}
