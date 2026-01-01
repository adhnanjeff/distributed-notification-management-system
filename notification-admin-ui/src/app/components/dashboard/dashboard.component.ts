import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, NotificationSummary, ChannelAnalytics, RecentNotification } from '../../services/api.service';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  summary: NotificationSummary | null = null;
  channelAnalytics: ChannelAnalytics[] = [];
  recentNotifications: RecentNotification[] = [];
  paginatedNotifications: RecentNotification[] = [];
  currentPage = 1;
  pageSize = 10;
  totalPages = 0;
  loading = true;
  error: string | null = null;

  private statusChart: Chart | null = null;
  private channelChart: Chart | null = null;

  constructor(private apiService: ApiService) {}

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    console.log('Loading dashboard data...');
    this.loading = true;
    this.error = null;

    // Load summary
    this.apiService.getSummary().subscribe({
      next: (data) => {
        console.log('Summary data received:', data);
        this.summary = data;
        this.createStatusChart();
      },
      error: (err) => {
        console.error('Summary error details:', {
          status: err.status,
          statusText: err.statusText,
          url: err.url,
          message: err.message,
          error: err.error
        });
        this.error = 'Failed to load summary data';
      }
    });

    // Load channel analytics
    this.apiService.getChannelAnalytics().subscribe({
      next: (data) => {
        console.log('Channel analytics received:', data);
        this.channelAnalytics = data;
        this.createChannelChart();
      },
      error: (err) => {
        console.error('Channel analytics error:', err);
        this.error = 'Failed to load channel analytics';
      }
    });

    // Load recent notifications
    this.apiService.getRecentNotifications().subscribe({
      next: (data) => {
        console.log('Recent notifications received:', data);
        this.recentNotifications = data;
        this.setupPagination();
        this.loading = false;
      },
      error: (err) => {
        console.error('Recent notifications error:', err);
        this.error = 'Failed to load recent notifications';
        this.loading = false;
      }
    });
  }

  private createStatusChart() {
    if (!this.summary) return;

    // Wait for DOM to be ready
    setTimeout(() => {
      const ctx = document.getElementById('statusChart') as HTMLCanvasElement;
      if (!ctx) {
        console.error('Status chart canvas not found');
        return;
      }

      if (this.statusChart) {
        this.statusChart.destroy();
      }

      // Add null check for TypeScript
      const summary = this.summary;
      if (!summary) return;

      this.statusChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
          labels: ['Sent', 'Failed', 'Pending'],
          datasets: [{
            data: [summary.sent, summary.failed, summary.pending],
            backgroundColor: ['#10B981', '#EF4444', '#F59E0B']
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              position: 'bottom'
            }
          }
        }
      });
    }, 100);
  }

  private createChannelChart() {
    if (!this.channelAnalytics.length) return;

    // Wait for DOM to be ready
    setTimeout(() => {
      const ctx = document.getElementById('channelChart') as HTMLCanvasElement;
      if (!ctx) {
        console.error('Channel chart canvas not found');
        return;
      }

      if (this.channelChart) {
        this.channelChart.destroy();
      }

      this.channelChart = new Chart(ctx, {
        type: 'bar',
        data: {
          labels: this.channelAnalytics.map(c => c.channel),
          datasets: [
            {
              label: 'Sent',
              data: this.channelAnalytics.map(c => c.sent),
              backgroundColor: '#10B981'
            },
            {
              label: 'Failed',
              data: this.channelAnalytics.map(c => c.failed),
              backgroundColor: '#EF4444'
            },
            {
              label: 'Pending',
              data: this.channelAnalytics.map(c => c.pending),
              backgroundColor: '#F59E0B'
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          scales: {
            y: {
              beginAtZero: true
            }
          }
        }
      });
    }, 150);
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'sent': return 'text-green-600';
      case 'failed': return 'text-red-600';
      case 'pending': return 'text-yellow-600';
      default: return 'text-gray-600';
    }
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString();
  }

  refresh() {
    this.loadData();
  }

  setupPagination() {
    this.totalPages = Math.ceil(this.recentNotifications.length / this.pageSize);
    this.updatePaginatedNotifications();
  }

  updatePaginatedNotifications() {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.paginatedNotifications = this.recentNotifications.slice(startIndex, endIndex);
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.updatePaginatedNotifications();
    }
  }

  nextPage() {
    this.goToPage(this.currentPage + 1);
  }

  prevPage() {
    this.goToPage(this.currentPage - 1);
  }

  ngOnDestroy() {
    if (this.statusChart) {
      this.statusChart.destroy();
    }
    if (this.channelChart) {
      this.channelChart.destroy();
    }
  }
}