using Terraria.NetWork.Concept.ContextBindingPrototype;

if (args.Any(static argument => string.Equals(argument, "--demo", StringComparison.OrdinalIgnoreCase)))
{
    RunDemo();
    return;
}

var external = new ExternalContextState();
var binder = new PacketContextBinder();
var lengths = new[] { 19, 20, 42, 70, 71 };
var lengthIndex = 2;
var requestedLength = lengths[lengthIndex];
WireSubmission? submission = null;
byte[]? committedFrame = null;
var lastResult = "ready";

while (true)
{
    Render(external, submission, requestedLength, committedFrame, lastResult);

    var key = Console.ReadKey(intercept: true).KeyChar;
    switch (char.ToLowerInvariant(key))
    {
        case 'b':
            submission = binder.Bind(external.Capture());
            lastResult = "bound a new immutable submission from the external snapshot";
            break;

        case 'm':
            external.Mutate();
            lastResult = "external context mutated; submission was not rebound";
            break;

        case 'l':
            lengthIndex = (lengthIndex + 1) % lengths.Length;
            requestedLength = lengths[lengthIndex];
            lastResult = $"requested variable segment length changed to {requestedLength}";
            break;

        case 'e':
            if (submission is null)
            {
                lastResult = "encode rejected: bind a submission first";
                break;
            }

            var attempt = SegmentComposer.TryEncode(submission, requestedLength);
            if (attempt.Committed)
            {
                committedFrame = attempt.Frame;
            }

            lastResult = attempt.Message + (attempt.Committed
                ? ""
                : "; previous committed frame was preserved");
            break;

        case 'r':
            external = new ExternalContextState();
            submission = null;
            committedFrame = null;
            lengthIndex = 2;
            requestedLength = lengths[lengthIndex];
            lastResult = "reset";
            break;

        case 'q':
            return;

        default:
            lastResult = $"unknown command '{key}'";
            break;
    }
}

static void Render(
    ExternalContextState external,
    WireSubmission? submission,
    int requestedLength,
    byte[]? committedFrame,
    string lastResult)
{
    try
    {
        Console.Clear();
    }
    catch (IOException)
    {
        // Redirected terminals may not support clearing; keep the prototype runnable.
    }

    Console.WriteLine("\x1b[1mContext Binding Prototype\x1b[0m");
    Console.WriteLine("throwaway demo: external data -> binder -> immutable submission -> staged encode");
    Console.WriteLine();

    Console.WriteLine("\x1b[1mExternal context\x1b[0m");
    Console.WriteLine($"  PlayerId     = {external.PlayerId}");
    Console.WriteLine($"  IncludeTick  = {external.IncludeTick}");
    Console.WriteLine($"  PayloadText  = {external.PayloadText}");
    Console.WriteLine($"  WorldTick    = {external.WorldTick}");
    Console.WriteLine();

    Console.WriteLine("\x1b[1mBound submission\x1b[0m");
    if (submission is null)
    {
        Console.WriteLine("  <none>");
    }
    else
    {
        Console.WriteLine($"  PlayerId     = {submission.PlayerId}");
        Console.WriteLine($"  Flags        = 0x{submission.Flags:X2}");
        Console.WriteLine($"  WorldTick    = {submission.WorldTick}");
        Console.WriteLine($"  PayloadText  = {submission.PayloadText}");
        Console.WriteLine($"  PayloadBytes = {submission.Payload.Length}");
    }

    Console.WriteLine();
    Console.WriteLine("\x1b[1mSegment contract\x1b[0m");
    Console.WriteLine($"  Fixed header = {SegmentComposer.FixedHeader}");
    Console.WriteLine($"  Variable    = {SegmentComposer.VariablePayload}");
    Console.WriteLine($"  Requested   = {requestedLength}");
    Console.WriteLine();

    Console.WriteLine("\x1b[1mCommitted final frame\x1b[0m");
    Console.WriteLine(committedFrame is null
        ? "  <none>"
        : $"  {committedFrame.Length} bytes: {Convert.ToHexString(committedFrame)}");
    Console.WriteLine();

    Console.WriteLine($"\x1b[1mLast result\x1b[0m: {lastResult}");
    Console.WriteLine();
    Console.WriteLine("\x1b[1mCommands\x1b[0m: [b] bind  [m] mutate external  [l] length  [e] encode  [r] reset  [q] quit");
}

static void RunDemo()
{
    var external = new ExternalContextState();
    var binder = new PacketContextBinder();
    var submission = binder.Bind(external.Capture());
    var committed = SegmentComposer.TryEncode(submission, 42);
    Require(committed.Committed && committed.Frame is not null, "binding and initial encode");

    var initialFrame = committed.Frame!;
    external.Mutate();
    var reboundFrame = SegmentComposer.TryEncode(submission, 42);
    Require(reboundFrame.Committed && reboundFrame.Frame is not null, "encode after external mutation");
    Require(initialFrame.SequenceEqual(reboundFrame.Frame), "external mutation does not change submission encoding");
    Require(submission.PlayerId == 7 && submission.WorldTick == 120, "submission remains bound to the original snapshot");
    Console.WriteLine("PASS: binding remains immutable after external mutation");

    var fixedBounds = SegmentBounds.Fixed(4);
    Require(fixedBounds.Accepts(4), "Fixed(4) accepts 4");
    Require(!fixedBounds.Accepts(3) && !fixedBounds.Accepts(5), "Fixed(4) rejects 3 and 5");
    Console.WriteLine("PASS: Fixed(4) boundaries");

    var bounded = SegmentBounds.Bounded(20, 70);
    Require(bounded.Accepts(20) && bounded.Accepts(42) && bounded.Accepts(70), "Bounded(20,70) accepts endpoints and middle");
    Require(!bounded.Accepts(19) && !bounded.Accepts(71), "Bounded(20,70) rejects outside values");
    Console.WriteLine("PASS: Bounded(20,70) boundaries");

    var rejected = SegmentComposer.TryEncode(submission, 19);
    Require(!rejected.Committed && rejected.Frame is null, "invalid segment is rejected before commit");
    Require(initialFrame.SequenceEqual(committed.Frame), "failed staging does not overwrite the previous final output");
    Console.WriteLine("PASS: rejected staging preserves the previous final output");
    Console.WriteLine("PASS: scripted context binding transcript");
}

static void Require(bool condition, string description)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Demo assertion failed: {description}");
    }
}
