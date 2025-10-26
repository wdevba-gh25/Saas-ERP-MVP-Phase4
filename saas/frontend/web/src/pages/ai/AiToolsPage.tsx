import { useState } from "react";
import PageLayout from "../../components/layout/PageLayout";
import { aiSummarize, aiExtract, aiRecommend } from "../../api/ai.api";
import { useAuthStore } from "../../store/auth.store";

const PLACEHOLDER_PROMPT =
  "Give me the sales projection for jeans and leggings for the next season, based on the latest trends and recommend me the best providers based on their performance during the season of the previous year.";

export default function AiToolsPage() {
  const { user } = useAuthStore();
  const [prompt, setPrompt] = useState(PLACEHOLDER_PROMPT);
  const [lockPrompt, setLockPrompt] = useState(false);
  const [busy, setBusy] = useState<"idle" | "summ" | "extr" | "reco">("idle");
  const [summary, setSummary] = useState<string>("");
  const [bullets, setBullets] = useState<string[]>([]);
  const [report, setReport] = useState<null | {
    title: string;
    summary: string;
    recommendations: string[];
    pdfUrl: string;
  }>(null);
  const [error, setError] = useState<string | null>(null);

  const doSummarize = async () => {
    setError(null); setBusy("summ");
    try {
//-------------->>>
      const projectId = user?.activeProjectId;
      if (!projectId) throw new Error("No active project selected");

      const result = await aiSummarize(projectId);
      setSummary(result.summary);

      // If backend also returned a PDF, reuse the report block for consistency
      if ((result as any).pdfUrl) {
        setReport({
          title: result.title ?? "Summary Report",
          summary: result.summary,
          recommendations: result.recommendations ?? [],
          pdfUrl: result.pdfUrl,
        });
      }
    } catch (e: any) {
      setError(e?.message || "Summarize failed");
    } finally { setBusy("idle"); }
  };

  const doExtract = async () => {
    setError(null); setBusy("extr");
    try {
      const projectId = user?.activeProjectId;
      if (!projectId) throw new Error("No active project selected");
      const { items } = await aiExtract(projectId);
      setBullets(items);
    } catch (e: any) {
      setError(e?.message || "Extract failed");
    } finally { setBusy("idle"); }
  };

  const doRecommend = async () => {
    setError(null); setBusy("reco");
    try {
      // The backend will IGNORE whatever we pass and use the fixed prompt for the demo
      const projectId = user?.activeProjectId;
      if (!projectId) throw new Error("No active project selected");
      const data = await aiRecommend(projectId, user?.organizationId);
      setReport(data);
      setLockPrompt(true);
    } catch (e: any) {
      setError(e?.message || "Next Season Report failed");
    } finally { setBusy("idle"); }
  };

  return (
    <PageLayout title="AI Tools">
      <div className="grid gap-6 max-w-3xl mx-auto">
        <div className="text-xs text-slate-400">
          Active ProjectId: {user?.activeProjectId ?? "null"}
        </div>
        <label className="block">
          <div className="mb-2 text-sm text-slate-300">Prompt</div>

        </label>
        <textarea
            className={`w-full min-h-[130px] rounded-xl p-3 outline-none transition-colors ${
              lockPrompt
                ? "bg-slate-600 text-slate-300 cursor-not-allowed"
                : "bg-slate-900/70 text-white"
            }`}
            placeholder={PLACEHOLDER_PROMPT}
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            readOnly={lockPrompt}
            disabled={lockPrompt}
          />
        <label className="flex items-center gap-2 text-slate-300">
          <input
            type="checkbox"
            checked={lockPrompt}
            onChange={(e) => setLockPrompt(e.target.checked)}
          />
          <span>Take this suggestion</span>
          <button
            onClick={doRecommend}
            disabled={busy !== "idle"}
            className="ml-auto btn-primary"
          >
            Next Season Report
          </button>
        </label>

        <div className="flex gap-2">
          <button onClick={doSummarize} disabled={busy !== "idle"} className="btn-secondary">/summarize</button>
          <button onClick={doExtract}   disabled={busy !== "idle"} className="btn-secondary">/extract</button>
        </div>

        {error && <div className="text-red-400 text-sm">{error}</div>}

        {summary && (
          <div className="rounded-xl p-4 bg-slate-800/40">
            <div className="font-semibold mb-2">Summary</div>
            <pre className="whitespace-pre-wrap text-slate-200">{summary}</pre>
          </div>
        )}

        {bullets.length > 0 && (
          <div className="rounded-xl p-4 bg-slate-800/40">
            <div className="font-semibold mb-2">Key points</div>
            <ul className="list-disc pl-5">
              {bullets.map((b, i) => <li key={i}>{b}</li>)}
            </ul>
          </div>
        )}

        {report && (
          <div className="rounded-xl p-4 bg-slate-800/40">
            <div className="font-semibold mb-2">{report.title}</div>
            <p className="mb-4">{report.summary}</p>
            <div className="mb-3 font-semibold">Recommendations</div>
            <ul className="list-disc pl-5 mb-4">
              {report.recommendations.map((r, i) => <li key={i}>{r}</li>)}
            </ul>
            <a
              className="btn-primary inline-block"
              href={`${import.meta.env.VITE_AI_REPORTS}${report.pdfUrl}`}
              target="_blank"
              rel="noreferrer"
            >
              Download PDF
            </a>
          </div>
        )}
      </div>
    </PageLayout>
  );
}
