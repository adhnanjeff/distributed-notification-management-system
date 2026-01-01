import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, DlqMessage } from '../../services/api.service';

@Component({
  selector: 'app-dlq-viewer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dlq-viewer.component.html',
  styleUrls: ['./dlq-viewer.component.css']
})
export class DlqViewerComponent implements OnInit {
  dlqMessages: DlqMessage[] = [];
  selectedSubscription = 'email-sub';
  subscriptions = ['email-sub', 'sms-sub', 'push-sub'];
  loading = false;
  error: string | null = null;
  replayingSubscription: string | null = null;

  constructor(private apiService: ApiService) {}

  ngOnInit() {
    this.loadDlqMessages();
  }

  loadDlqMessages() {
    console.log('Loading DLQ messages for subscription:', this.selectedSubscription);
    this.loading = true;
    this.error = null;

    this.apiService.getDlqMessages(this.selectedSubscription).subscribe({
      next: (data) => {
        console.log('DLQ messages received:', data);
        this.dlqMessages = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('DLQ error details:', {
          status: err.status,
          statusText: err.statusText,
          url: err.url,
          message: err.message,
          error: err.error
        });
        this.error = `Failed to load DLQ messages: ${err.status} ${err.statusText}`;
        this.loading = false;
      }
    });
  }

  onSubscriptionChange() {
    this.loadDlqMessages();
  }

  replayMessages() {
    if (this.replayingSubscription) return;

    this.replayingSubscription = this.selectedSubscription;
    this.error = null;

    this.apiService.replayDlqMessages(this.selectedSubscription).subscribe({
      next: (response) => {
        alert('DLQ messages replayed successfully!');
        this.loadDlqMessages(); // Refresh the list
        this.replayingSubscription = null;
      },
      error: (err) => {
        this.error = `Failed to replay messages: ${err.message}`;
        this.replayingSubscription = null;
        console.error(err);
      }
    });
  }

  formatJson(jsonString: string): string {
    try {
      const obj = JSON.parse(jsonString);
      return JSON.stringify(obj, null, 2);
    } catch {
      return jsonString;
    }
  }

  refresh() {
    this.loadDlqMessages();
  }
}