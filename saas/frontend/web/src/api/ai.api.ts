import axios from "axios";
import { useAuthStore } from "../store/auth.store";

const AI_BASE = import.meta.env.VITE_AI_BASE; // http://localhost:8009

export const aiClient = axios.create({
  baseURL: AI_BASE,
  headers: { "Content-Type": "application/json" },
  withCredentials: false,
});

// Attach JWT if you want (optional for demo; keeps shape aligned with others)
aiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) {
    config.headers = config.headers ?? {};
    (config.headers as any).Authorization = `Bearer ${token}`;
  }
  return config;
});

//----------------->>>>
export interface SummarizeResponse {
  title: string;
  summary: string;
  recommendations: string[];
  pdfUrl: string;
}

//-----------
export async function aiSummarize(projectId: string): Promise<SummarizeResponse> {
  const { data } = await aiClient.post<SummarizeResponse>("/summarize", { projectId });
  return data;
}

export async function aiExtract(projectId: string) {
  const { data } = await aiClient.post<{ items: string[] }>("/extract", { projectId });
  return data;
}

export type RecommendResponse = {
  title: string;
  summary: string;
  recommendations: string[];
  pdfUrl: string; // relative to AI_BASE
};

export async function aiRecommend(projectId: string, orgId?: string) {
  // NOTE: operatorPrompt intentionally ignored by backend for demo
  const { data } = await aiClient.post<RecommendResponse>("/recommend", {
    operatorPrompt: projectId,
    organizationId: orgId,
  });
  return data;
}