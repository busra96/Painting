using UnityEngine;

public sealed partial class ImpastoPaintSurface
{
    // A reusable contact workspace, not a hidden paint reservoir. Each contact gathers then returns
    // its paint within the same operation, so all persistent paint always belongs to the canvas.
    public sealed class KnifeLoad
    {
        internal struct Transfer
        {
            public float x, y, amount, coat;
            public Vector3 mass, coatMass;
            public int order;
        }
        internal Transfer[] transfers = new Transfer[0];
        internal Vector2 axis, travel;
        internal bool hasAxis;
        internal float untilNextContact;
        internal float contactBed;
        public float Volume => 0;
        public float ContactVolume { get; internal set; }
        public Vector2 Direction => travel;
        public Color PaintColor { get; internal set; }

        internal void Prepare(int capacity)
        {
            if (transfers.Length < capacity) transfers = new Transfer[capacity];
            hasAxis = false;
            untilNextContact = 0;
            ContactVolume = 0;
            contactBed = 0;
            PaintColor = Color.clear;
        }
    }

    public KnifeLoad BeginKnife(KnifeLoad load, float bladeWidth)
    {
        if (load == null) load = new KnifeLoad();
        float reach = bladeWidth * .8f + CellSize * 3;
        int columns = Mathf.Min(width, Mathf.CeilToInt(reach * 2 / VDx) + 3);
        int rows = Mathf.Min(height, Mathf.CeilToInt(reach * 2 / VDy) + 3);
        load.Prepare(columns * rows);
        return load;
    }

    public void KnifeSweep(Vector3 from, Vector3 to, float bladeWidth, float filmScale, float pickup, KnifeLoad load)
    {
        if (!IsReady || IsSpreading || load == null || load.transfers.Length == 0) return;
        Vector2 a = GridPoint(from), b = GridPoint(to);
        Vector2 delta = new Vector2((b.x - a.x) * VDx, (b.y - a.y) * VDy);
        float distance = delta.magnitude;
        if (distance < .000001f) return;
        using (KnifeMarker.Auto())
        {
            Vector2 travel = delta / distance, axis = new Vector2(-travel.y, travel.x);
            if (load.hasAxis && Vector2.Dot(axis, load.axis) < 0) axis = -axis;
            if (!load.hasAxis) load.axis = axis;
            load.hasAxis = true;
            float step = CellSize * .75f;
            float turn = 1 - Mathf.Exp(-step / Mathf.Max(CellSize, bladeWidth * .035f));
            float cursor = load.untilNextContact;
            for (; cursor <= distance; cursor += step)
            {
                load.axis = Vector2.Lerp(load.axis, axis, turn).normalized;
                Vector2 along = new Vector2(load.axis.y, -load.axis.x);
                if (Vector2.Dot(along, travel) < 0) along = -along;
                load.travel = along;
                KnifeContact(Vector2.Lerp(a, b, cursor / distance), load.axis, along,
                    bladeWidth, filmScale, pickup, step, load);
            }
            load.untilNextContact = cursor - distance;
        }
    }

    private void KnifeContact(Vector2 center, Vector2 axis, Vector2 along, float bladeWidth,
        float filmScale, float pickup, float step, KnifeLoad load)
    {
        float vdx = VDx, vdy = VDy, cell = CellSize;
        float half = Mathf.Max(cell, bladeWidth * .5f), depth = Mathf.Max(cell * 3, bladeWidth * .28f);
        // Enclose the oriented contact pad, rather than scanning a square around the whole blade.
        float halfDepth=(depth+bladeWidth*.1f)*.5f;
        Vector2 padCenter=center-new Vector2(along.x*halfDepth/vdx,along.y*halfDepth/vdy);
        float rx=(Mathf.Abs(axis.x)*half+Mathf.Abs(along.x)*halfDepth+cell*3)/vdx;
        float ry=(Mathf.Abs(axis.y)*half+Mathf.Abs(along.y)*halfDepth+cell*3)/vdy;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(padCenter.x-rx));
        int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(padCenter.x+rx));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(padCenter.y-ry));
        int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(padCenter.y+ry));
        if (x0 > x1 || y0 > y1) return;
        float force = 1 - Mathf.Exp(-Mathf.Clamp01(pickup) * step / cell * 2.8f);
        float footprintArea=Mathf.Max(.000001f,bladeWidth*depth/(VisualScale.x*VisualScale.y));
        float baseBed=Film*Mathf.Max(.05f,filmScale)*.85f;
        float desiredBed=baseBed+load.ContactVolume/footprintArea*.4f*Mathf.Clamp(filmScale,.25f,2);
        load.contactBed=Mathf.Lerp(load.contactBed>0?load.contactBed:baseBed,desiredBed,.35f);
        float bed=load.contactBed;
        float lateral = 1 + step / depth * Mathf.Lerp(.12f, .65f, Mathf.Clamp01(pickup));
        int count = 0;
        float movedVolume = 0;
        Vector3 movedMass = Vector3.zero;
        bool captured = false;

        // Gather all source cells before depositing any of them. This prevents a scan-direction
        // bias and avoids moving the same incoming paint repeatedly through a single contact.
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            int i = y * width + x;
            if (thickness[i] <= bed) continue;
            Vector2 d = new Vector2((x - center.x) * vdx, (y - center.y) * vdy);
            float across = Vector2.Dot(d, axis), u = across / half;
            if (Mathf.Abs(u) >= 1) continue;
            float bow = bladeWidth * .10f * u * u;
            float longitudinal = Vector2.Dot(d, along), behind = -(longitudinal + bow);
            if (behind < 0 || behind >= depth) continue;
            float shoulder = Mathf.Clamp01((1 - u * u * u * u) * 3);
            float contact = shoulder * Mathf.SmoothStep(0, 1, (depth - behind) / (depth * .4f));
            float texturedBed=bed*(1+(contactTexture[i]-.5f)*Variation*.25f);
            float fraction = Mathf.Clamp01((thickness[i] - texturedBed) / thickness[i] * force * contact);
            if (fraction <= .000001f) continue;
            if (!captured) { CaptureHistoryTiles(x0, y0, x1, y1); captured = true; }
            int order = surfaceOrder[i];
            TakeFromCell(i, fraction, out float taken, out Vector3 mass, out float coat, out Vector3 coatMass);
            float targetAcross = Mathf.Clamp(across * lateral, -half * .98f, half * .98f);
            float targetU = targetAcross / half;
            float front = -bladeWidth * .10f * targetU * targetU + cell * .6f;
            float targetAlong = Mathf.Min(front, longitudinal + depth * .72f);
            load.transfers[count++] = new KnifeLoad.Transfer
            {
                x = center.x + (axis.x * targetAcross + along.x * targetAlong) / vdx,
                y = center.y + (axis.y * targetAcross + along.y * targetAlong) / vdy,
                amount = taken, mass = mass, coat = coat, coatMass = coatMass, order = order
            };
            movedVolume += taken * cellArea;
            movedMass += mass * cellArea;
        }
        for (int n = 0; n < count; n++)
        {
            KnifeLoad.Transfer p = load.transfers[n];
            SplatBilinear(p.x, p.y, p.amount, p.mass, p.coat, p.coatMass, p.order);
        }
        load.ContactVolume = movedVolume;
        if (movedVolume <= 0) { load.PaintColor = Color.clear; return; }
        Vector3 absorption = movedMass / movedVolume;
        load.PaintColor = new Color(Mathf.Exp(-absorption.x), Mathf.Exp(-absorption.y), Mathf.Exp(-absorption.z), 1).gamma;

        // Flatten the metal contact locally. The paint remains on the canvas while the edge pushes
        // it forward and outward; there is no invisible load to dump when the pointer is released.
        float smoothing = Mathf.Lerp(.12f, .28f, Mathf.Clamp01(1.5f - filmScale));
        int samples = Mathf.CeilToInt(bladeWidth / cell);
        for (int k = 0; k < samples; k++)
        {
            float u = (k + .5f) / samples * 2 - 1, offset = u * half;
            for (int n = 0; n < 4; n++)
            {
                float behind = -bladeWidth * .10f * u * u - cell * (n + .5f);
                int x = Mathf.RoundToInt(center.x + (axis.x * offset + along.x * behind) / vdx);
                int y = Mathf.RoundToInt(center.y + (axis.y * offset + along.y * behind) / vdy);
                if (x < 0 || y < 0 || x >= width - 1 || y >= height - 1) continue;
                int i = y * width + x;
                RelaxPressedPair(i, i + 1, smoothing); RelaxPressedPair(i, i + width, smoothing);
            }
        }
        ActivateSettling(x0, y0, x1, y1); IncludeChanged(x0, y0, x1, y1);
        dirty = true;
    }

    public void EndKnife(KnifeLoad load, float bladeWidth)
    {
        // Lifting changes only the tool state. The final image is exactly the image seen while
        // dragging: no crescent, extra ridge, backward stamp or disconnected paint fragment.
        if (load == null) return;
        load.hasAxis = false;
        load.ContactVolume = 0;
        load.PaintColor = Color.clear;
    }
}
