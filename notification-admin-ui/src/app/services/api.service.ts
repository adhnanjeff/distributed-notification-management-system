import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface NotificationSummary {
  total: number;
  sent: number;
  failed: number;
  pending: number;
  averageProcessingTimeMs: number;
}

export interface ChannelAnalytics {
  channel: string;
  total: number;
  sent: number;
  failed: number;
  pending: number;
}

export interface RecentNotification {
  id: string;
  channel: string;
  status: string;
  createdAt: string;
  processedAt?: string;
}

export interface DlqMessage {
  messageId: string;
  body: string;
  reason: string;
  error: string;
  deliveryCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'https://localhost:5001/api';

  constructor(private http: HttpClient) {
    console.log('ApiService initialized with baseUrl:', this.baseUrl);
  }

  getSummary(): Observable<NotificationSummary> {
    console.log('Making request to:', `${this.baseUrl}/analytics/summary`);
    return this.http.get<NotificationSummary>(`${this.baseUrl}/analytics/summary`);
  }

  getChannelAnalytics(): Observable<ChannelAnalytics[]> {
    console.log('Making request to:', `${this.baseUrl}/analytics/by-channel`);
    return this.http.get<ChannelAnalytics[]>(`${this.baseUrl}/analytics/by-channel`);
  }

  getRecentNotifications(): Observable<RecentNotification[]> {
    console.log('Making request to:', `${this.baseUrl}/analytics/recent`);
    return this.http.get<RecentNotification[]>(`${this.baseUrl}/analytics/recent`);
  }

  getDlqMessages(subscription: string): Observable<DlqMessage[]> {
    console.log('Making request to:', `${this.baseUrl}/dlq/${subscription}`);
    return this.http.get<DlqMessage[]>(`${this.baseUrl}/dlq/${subscription}`);
  }

  replayDlqMessages(subscription: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/dlq/replay/${subscription}`, {}, { responseType: 'text' });
  }
}